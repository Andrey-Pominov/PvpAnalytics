using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Core.Logs;
using PvpAnalytics.Core.Models;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Ingests combat log files in Lua table format.
/// </summary>
public class LuaCombatLogIngestionService(
    IRepository<Player> playerRepo,
    IRepository<Match> matchRepo,
    IRepository<MatchResult> resultRepo,
    IRepository<CombatLogEntry> entryRepo,
    IWowApiService wowApiService,
    ILogger<LuaCombatLogIngestionService> logger)
    : ICombatLogIngestionService
{
    private readonly PlayerCache _playerCache = new PlayerCache(playerRepo);
    private readonly Dictionary<string, string> _playerRegions = new(StringComparer.OrdinalIgnoreCase);

    public async Task<List<Match>> IngestAsync(Stream fileStream, CancellationToken ct = default)
    {
        logger.LogInformation("Lua combat log ingestion started.");

        // Parse Lua table structure
        var luaMatches = LuaTableParser.Parse(fileStream);
        logger.LogInformation("Parsed {MatchCount} match(es) from Lua table.", luaMatches.Count);

        var allPersistedMatches = new List<Match>();

        foreach (var luaMatch in luaMatches)
        {
            try
            {
                var persistedMatch = await ProcessLuaMatchAsync(luaMatch, ct);
                if (persistedMatch is { Id: > 0 })
                {
                    allPersistedMatches.Add(persistedMatch);
                    logger.LogInformation("Persisted match {MatchId} from Lua format.", persistedMatch.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process Lua match with zone {Zone}, start {StartTime}",
                    luaMatch.Zone, luaMatch.StartTime);
            }
        }

        // Batch persist all pending creates and updates at file end
        var pendingPlayerNames = _playerCache.GetPendingCreates().Keys.ToList();
        await _playerCache.BatchPersistAsync(ct);

        // Enrich players with WoW API data if missing information
        await EnrichPlayersWithWowApiAsync(pendingPlayerNames, ct);

        // Persist any updates from enrichment
        await _playerCache.BatchPersistAsync(ct);

        logger.LogInformation("Lua combat log ingestion completed. Persisted {MatchCount} match(es).",
            allPersistedMatches.Count);
        return allPersistedMatches;
    }

    private async Task<Match?> ProcessLuaMatchAsync(LuaMatchData luaMatch, CancellationToken ct)
    {
        if (!TryParseTimestamps(luaMatch, out var startTime, out var endTime))
        {
            return null;
        }

        var arenaZone = MapZoneNameToArenaZone(luaMatch.Zone);
        var gameMode = ParseGameMode(luaMatch.Mode);
        var matchState = new LuaMatchProcessingState();

        CollectPlayersForLookup(luaMatch.Logs, startTime);
        await _playerCache.BatchLookupAsync(ct);
        ProcessCombatEvents(luaMatch.Logs, startTime, matchState);
        await UpdatePlayersFromSpellsAsync(matchState.PlayersByKey, matchState.PlayerSpells, ct);

        var arenaMatchId = GenerateArenaMatchId(luaMatch, startTime, endTime);
        var mapName = ArenaZoneIds.GetDisplayName(arenaZone);

        return await FinalizeAndPersistAsync(
            arenaZone,
            startTime,
            endTime,
            matchState.Participants,
            matchState.BufferedEntries,
            matchState.PlayersByKey,
            matchState.PlayerSpells,
            ct,
            gameMode,
            arenaMatchId,
            mapName);
    }

    private bool TryParseTimestamps(LuaMatchData luaMatch, out DateTime startTime, out DateTime endTime)
    {
        if (TryParseDateTime(luaMatch.StartTime, out startTime) &&
            TryParseDateTime(luaMatch.EndTime, out endTime))
        {
            return true;
        }

        logger.LogWarning("Failed to parse timestamps for match. Start: {Start}, End: {End}",
            luaMatch.StartTime, luaMatch.EndTime);
        endTime = DateTime.MinValue;
        return false;
    }

    private void CollectPlayersForLookup(List<string> logLines, DateTime startTime)
    {
        foreach (var logLine in logLines)
        {
            var parsed = SimplifiedLogParser.ParseLine(logLine, startTime);
            if (parsed == null) continue;

            AddPlayerToPendingIfValid(parsed.SourceName);
            AddPlayerToPendingIfValid(parsed.TargetName);
        }
    }

    private void AddPlayerToPendingIfValid(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return;

        var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(fullName);
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrWhiteSpace(realm))
        {
            return;
        }

        _playerCache.GetOrAddPending(playerName, realm);
        var region = ExtractRegion(fullName);
        if (!string.IsNullOrEmpty(region))
        {
            _playerRegions[playerName] = region;
        }
    }

    private void ProcessCombatEvents(List<string> logLines, DateTime startTime, LuaMatchProcessingState state)
    {
        foreach (var logLine in logLines)
        {
            var parsed = SimplifiedLogParser.ParseLine(logLine, startTime);
            if (parsed == null) continue;

            ProcessParsedEvent(parsed, state.PlayersByKey, state.Participants, state.BufferedEntries,
                state.PlayerSpells);
        }
    }

    private sealed class LuaMatchProcessingState
    {
        public Dictionary<string, Player> PlayersByKey { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Participants { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CombatLogEntry> BufferedEntries { get; } = new();
        public Dictionary<string, HashSet<string>> PlayerSpells { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private void ProcessParsedEvent(
        ParsedCombatLogEvent parsed,
        Dictionary<string, Player> playersByKey,
        HashSet<string> participants,
        List<CombatLogEntry> bufferedEntries,
        Dictionary<string, HashSet<string>> playerSpells)
    {
        ProcessPlayer(parsed.SourceName, parsed.SpellName, playersByKey, participants, playerSpells);
        ProcessPlayer(parsed.TargetName, null, playersByKey, participants, playerSpells);
        CreateCombatLogEntryIfValid(parsed, playersByKey, bufferedEntries);
    }

    private void ProcessPlayer(
        string? fullName,
        string? spellName,
        Dictionary<string, Player> playersByKey,
        HashSet<string> participants,
        Dictionary<string, HashSet<string>> playerSpells)
    {
        if (string.IsNullOrEmpty(fullName))
            return;

        var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(fullName);
        if (string.IsNullOrEmpty(playerName))
        {
         return;   
        }
        
        var cached = _playerCache.GetCached(playerName);
        if (cached != null)
        {
            playersByKey.TryAdd(playerName, cached);
            participants.Add(playerName);
        }
        else if (!string.IsNullOrWhiteSpace(realm))
        {
            AddPlayerToPending(playerName, realm, fullName);
            participants.Add(playerName);
        }

        if (!string.IsNullOrEmpty(spellName))
        {
            TrackSpellForPlayer(playerName, spellName, playerSpells);
        }
    }

    private void AddPlayerToPending(string playerName, string realm, string fullName)
    {
        _playerCache.GetOrAddPending(playerName, realm);
        var region = ExtractRegion(fullName);
        if (!string.IsNullOrEmpty(region))
        {
            _playerRegions[playerName] = region;
        }
    }

    private static void TrackSpellForPlayer(
        string playerName,
        string spellName,
        Dictionary<string, HashSet<string>> playerSpells)
    {
        if (!playerSpells.TryGetValue(playerName, out var spells))
        {
            spells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            playerSpells[playerName] = spells;
        }

        spells.Add(spellName);
    }

    private static void CreateCombatLogEntryIfValid(
        ParsedCombatLogEvent parsed,
        Dictionary<string, Player> playersByKey,
        List<CombatLogEntry> bufferedEntries)
    {
        if (string.IsNullOrEmpty(parsed.SourceName))
            return;

        var (sourceName, _) = PlayerInfoExtractor.ParsePlayerName(parsed.SourceName);
        if (string.IsNullOrEmpty(sourceName) || !playersByKey.TryGetValue(sourceName, out var source))
            return;

        if (source.Id <= 0)
            return;

        var target = GetTargetPlayer(parsed.TargetName, playersByKey);
        bufferedEntries.Add(new CombatLogEntry
        {
            Timestamp = parsed.Timestamp,
            SourcePlayerId = source.Id,
            TargetPlayerId = target?.Id,
            Ability = parsed.SpellName ?? parsed.EventType,
            DamageDone = parsed.Damage ?? 0,
            HealingDone = parsed.Healing ?? 0,
            CrowdControl = string.Empty
        });
    }

    private static Player? GetTargetPlayer(string? targetName, Dictionary<string, Player> playersByKey)
    {
        if (string.IsNullOrEmpty(targetName))
            return null;

        var (targetPlayerName, _) = PlayerInfoExtractor.ParsePlayerName(targetName);
        return !string.IsNullOrEmpty(targetPlayerName) && playersByKey.TryGetValue(targetPlayerName, out var target)
            ? target
            : null;
    }

    private static bool TryParseDateTime(string? dateTimeStr, out DateTime dateTime)
    {
        dateTime = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(dateTimeStr))
            return false;

        // Try format: "2025-11-20 00:33:57"
        if (DateTime.TryParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out dateTime))
        {
            return true;
        }

        // Fallback to general parsing
        return DateTime.TryParse(dateTimeStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out dateTime);
    }

    private static ArenaZone MapZoneNameToArenaZone(string? zoneName)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
            return ArenaZone.Unknown;

        // Map zone names to ArenaZone enum
        return zoneName.ToLowerInvariant() switch
        {
            "dornogal" => ArenaZone.MaldraxxusColiseum, // Dornogal is Maldraxxus Coliseum
            "blood ring" => ArenaZone.BloodRing,
            "dalaran arena" => ArenaZone.DalaranArena,
            "ring of valor" => ArenaZone.RingOfValor,
            "ruins of lordaeron" => ArenaZone.RuinsOfLordaeron,
            "nagrand arena" => ArenaZone.NagrandArena,
            "mugambala" => ArenaZone.Mugambala,
            "the tiger's peak" => ArenaZone.TheTigersPeak,
            "tol'viron arena" => ArenaZone.TolvironArena,
            "black rook hold arena" => ArenaZone.BlackRookHoldArena,
            "maldraxxus coliseum" => ArenaZone.MaldraxxusColiseum,
            _ => ArenaZone.Unknown
        };
    }

    private static GameMode ParseGameMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return GameMode.TwoVsTwo;

        return mode.ToLowerInvariant() switch
        {
            "2v2" => GameMode.TwoVsTwo,
            "3v3" => GameMode.ThreeVsThree,
            "skirmish" => GameMode.Skirmish,
            "rbg" => GameMode.Rbg,
            "shuffle" => GameMode.Shuffle,
            _ => GameMode.TwoVsTwo
        };
    }

    private static string GenerateArenaMatchId(LuaMatchData luaMatch, DateTime startTime, DateTime endTime)
    {
        // Generate a unique match ID based on zone, timestamps, and mode
        var baseStr = $"{luaMatch.Zone}_{startTime:yyyyMMddHHmmss}_{endTime:yyyyMMddHHmmss}_{luaMatch.Mode}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(baseStr)))[..16];
    }

    private static string ExtractRegion(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var trimmed = fullName.Trim('"', ' ');
        var regionSuffixes = new[] { "-EU", "-US", "-KR", "-TW", "-CN" };
        foreach (var suffix in regionSuffixes)
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return suffix[1..].ToLowerInvariant();
            }
        }

        return "eu"; // Default to EU
    }

    private Task UpdatePlayersFromSpellsAsync(
        Dictionary<string, Player> playersByKey,
        Dictionary<string, HashSet<string>> playerSpells,
        CancellationToken ct)
    {
        foreach (var (playerName, spells) in playerSpells)
        {
            if (!playersByKey.TryGetValue(playerName, out var player) || spells.Count == 0)
                continue;

            var originalClass = player.Class;
            var originalFaction = player.Faction;
            var originalSpec = player.Spec;

            PlayerInfoExtractor.UpdatePlayerFromSpells(player, spells);

            if (player.Class == originalClass && player.Faction == originalFaction &&
                player.Spec == originalSpec) continue;
            _playerCache.MarkForUpdate(player);
            logger.LogDebug("Marked player {PlayerName} for update: Class={Class}, Faction={Faction}, Spec={Spec}",
                player.Name, player.Class, player.Faction, player.Spec);
        }

        return Task.CompletedTask;
    }

    private async Task EnrichPlayersWithWowApiAsync(List<string> playerNamesToEnrich, CancellationToken ct)
    {
        // Note: Spec is not available from WoW API, so we only enrich for missing Class/Faction
        var playersToEnrich = GetPlayersNeedingEnrichment(playerNamesToEnrich);

        foreach (var player in playersToEnrich)
        {
            await EnrichSinglePlayerAsync(player, ct);
        }
    }

    private List<Player> GetPlayersNeedingEnrichment(List<string> playerNamesToEnrich)
    {
        return playerNamesToEnrich
            .Select(name => _playerCache.GetCached(name))
            .OfType<Player>()
            .Where(cached => string.IsNullOrWhiteSpace(cached.Class) || string.IsNullOrWhiteSpace(cached.Faction))
            .ToList();
    }

    private async Task EnrichSinglePlayerAsync(Player player, CancellationToken ct)
    {
        try
        {
            var region = _playerRegions.GetValueOrDefault(player.Name, "eu");
            var apiData = await wowApiService.GetPlayerDataAsync(player.Realm, player.Name, region, ct);

            if (apiData == null)
                return;

            var updated = UpdatePlayerFromApiData(player, apiData);
            if (updated)
            {
                _playerCache.MarkForUpdate(player);
                logger.LogDebug("Enriched player {PlayerName} from WoW API: Class={Class}, Faction={Faction}",
                    player.Name, player.Class, player.Faction);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enrich player {PlayerName} from WoW API", player.Name);
        }
    }

    private static bool UpdatePlayerFromApiData(Player player, WowPlayerData apiData)
    {
        var needsUpdate = false;

        if (string.IsNullOrWhiteSpace(player.Class) && !string.IsNullOrWhiteSpace(apiData.Class))
        {
            player.Class = apiData.Class;
            needsUpdate = true;
        }

        if (string.IsNullOrWhiteSpace(player.Faction) && !string.IsNullOrWhiteSpace(apiData.Faction))
        {
            player.Faction = apiData.Faction;
            needsUpdate = true;
        }

        return needsUpdate;
    }

    private async Task<Match> FinalizeAndPersistAsync(
        ArenaZone arenaZone,
        DateTime? start,
        DateTime? end,
        HashSet<string> participants,
        List<CombatLogEntry> entries,
        Dictionary<string, Player> playersByKey,
        Dictionary<string, HashSet<string>> playerSpells,
        CancellationToken ct,
        GameMode gameMode,
        string arenaMatchId,
        string mapName)
    {
        var uniqueHash = ComputeMatchHash(participants, start, end, arenaMatchId);
        var match = CreateMatchEntity(arenaZone, start, end, arenaMatchId, gameMode, mapName, uniqueHash);

        match = await PersistMatchWithDuplicateHandlingAsync(match, uniqueHash, participants.Count, ct);
        await PersistCombatLogEntriesAsync(entries, match.Id, ct);
        await PersistMatchResultsAsync(participants, playersByKey, playerSpells, match.Id, ct);

        return match;
    }

    private static Match CreateMatchEntity(
        ArenaZone arenaZone,
        DateTime? start,
        DateTime? end,
        string arenaMatchId,
        GameMode gameMode,
        string mapName,
        string uniqueHash)
    {
        return new Match
        {
            CreatedOn = start ?? DateTime.UtcNow,
            MapName = mapName,
            ArenaZone = arenaZone,
            ArenaMatchId = arenaMatchId,
            GameMode = gameMode,
            Duration = start.HasValue && end.HasValue ? (long)(end.Value - start.Value).TotalSeconds : 0,
            IsRanked = true,
            UniqueHash = uniqueHash
        };
    }

    private async Task<Match> PersistMatchWithDuplicateHandlingAsync(
        Match match,
        string uniqueHash,
        int participantCount,
        CancellationToken ct)
    {
        try
        {
            match = await matchRepo.AddAsync(match, ct);
            logger.LogInformation(
                "Persisted new match {MatchId} with UniqueHash {UniqueHash} and {ParticipantCount} participants.",
                match.Id, uniqueHash, participantCount);
            return match;
        }
        catch (Exception ex)
        {
            if (IsUniqueConstraintViolation(ex))
            {
                return await HandleDuplicateMatchAsync(uniqueHash, ct);
            }

            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        return ex.GetType().FullName?.Contains("DbUpdateException") == true &&
               ex.InnerException?.GetType().FullName?.Contains("PostgresException") == true &&
               (ex.InnerException.GetType().GetProperty("SqlState")?.GetValue(ex.InnerException)?.ToString() ==
                "23505" ||
                ex.Message.Contains("23505") ||
                ex.Message.Contains("duplicate key value") ||
                ex.Message.Contains("IX_Matches_UniqueHash"));
    }

    private async Task<Match> HandleDuplicateMatchAsync(string uniqueHash, CancellationToken ct)
    {
        logger.LogInformation(
            "Match with UniqueHash {UniqueHash} already exists in database, returning existing match.", uniqueHash);
        var existingMatches = await matchRepo.ListAsync(m => m.UniqueHash == uniqueHash, ct);
        return existingMatches.Count > 0
            ? existingMatches[0]
            : throw new InvalidOperationException(
                $"Match with UniqueHash {uniqueHash} was reported as duplicate but not found in database.");
    }

    private async Task PersistCombatLogEntriesAsync(List<CombatLogEntry> entries, long matchId, CancellationToken ct)
    {
        foreach (var e in entries)
        {
            e.MatchId = matchId;
            await entryRepo.AddAsync(e, ct);
        }
    }

    private async Task PersistMatchResultsAsync(
        HashSet<string> participants,
        Dictionary<string, Player> playersByKey,
        Dictionary<string, HashSet<string>> playerSpells,
        long matchId,
        CancellationToken ct)
    {
        foreach (var name in participants)
        {
            if (!playersByKey.TryGetValue(name, out var player))
                continue;

            var matchSpec = GetMatchSpecForPlayer(name, playerSpells);
            await resultRepo.AddAsync(new MatchResult
            {
                MatchId = matchId,
                PlayerId = player.Id,
                Team = "Unknown",
                RatingBefore = 0,
                RatingAfter = 0,
                IsWinner = false,
                Spec = matchSpec
            }, ct);
        }
    }

    private static string GetMatchSpecForPlayer(string playerName, Dictionary<string, HashSet<string>> playerSpells)
    {
        return playerSpells.TryGetValue(playerName, out var spells)
            ? PlayerInfoExtractor.DetermineSpecForMatch(spells)
            : string.Empty;
    }

    private static string ComputeMatchHash(IEnumerable<string> playerKeys, DateTime? start, DateTime? end,
        string? arenaMatchId = null)
    {
        var baseStr = string.Join('|', playerKeys.OrderBy(x => x)) + $"|{start:O}|{end:O}|{arenaMatchId}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(baseStr)));
    }
}