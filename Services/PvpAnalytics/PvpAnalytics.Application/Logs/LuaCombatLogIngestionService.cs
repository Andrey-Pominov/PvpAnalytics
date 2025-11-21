using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Core.Logs;
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
                if (persistedMatch != null && persistedMatch.Id > 0)
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
        // Parse timestamps
        if (!TryParseDateTime(luaMatch.StartTime, out var startTime) ||
            !TryParseDateTime(luaMatch.EndTime, out var endTime))
        {
            logger.LogWarning("Failed to parse timestamps for match. Start: {Start}, End: {End}",
                luaMatch.StartTime, luaMatch.EndTime);
            return null;
        }

        // Map zone name to ArenaZone enum
        var arenaZone = MapZoneNameToArenaZone(luaMatch.Zone);

        // Parse game mode
        var gameMode = ParseGameMode(luaMatch.Mode);

        // Process logs
        var playersByKey = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        var participants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bufferedEntries = new List<CombatLogEntry>();
        var playerSpells = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // First pass: collect player names and add to pending creates
        foreach (var logLine in luaMatch.Logs)
        {
            var parsed = SimplifiedLogParser.ParseLine(logLine, startTime);
            if (parsed == null) continue;

            // Add players to pending creates for batch lookup
            if (!string.IsNullOrEmpty(parsed.SourceName))
            {
                var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(parsed.SourceName);
                var region = ExtractRegion(parsed.SourceName);
                if (!string.IsNullOrEmpty(playerName) && !string.IsNullOrWhiteSpace(realm))
                {
                    _playerCache.GetOrAddPending(playerName, realm);
                    if (!string.IsNullOrEmpty(region))
                    {
                        _playerRegions[playerName] = region;
                    }
                }
            }
            if (!string.IsNullOrEmpty(parsed.TargetName))
            {
                var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(parsed.TargetName);
                var region = ExtractRegion(parsed.TargetName);
                if (!string.IsNullOrEmpty(playerName) && !string.IsNullOrWhiteSpace(realm))
                {
                    _playerCache.GetOrAddPending(playerName, realm);
                    if (!string.IsNullOrEmpty(region))
                    {
                        _playerRegions[playerName] = region;
                    }
                }
            }
        }

        // Batch lookup players
        await _playerCache.BatchLookupAsync(ct);

        // Second pass: process events and create entries
        foreach (var logLine in luaMatch.Logs)
        {
            var parsed = SimplifiedLogParser.ParseLine(logLine, startTime);
            if (parsed == null) continue;

            ProcessParsedEvent(parsed, playersByKey, participants, bufferedEntries, playerSpells);
        }

        // Update players from spells
        await UpdatePlayersFromSpellsAsync(playersByKey, playerSpells, ct);

        // Generate arena match ID if not available
        var arenaMatchId = GenerateArenaMatchId(luaMatch, startTime, endTime);

        // Finalize and persist match
        var match = await FinalizeAndPersistAsync(
            arenaZone,
            startTime,
            endTime,
            participants,
            bufferedEntries,
            playersByKey,
            playerSpells,
            ct,
            gameMode,
            arenaMatchId);

        return match;
    }

    private void ProcessParsedEvent(
        ParsedCombatLogEvent parsed,
        Dictionary<string, Player> playersByKey,
        HashSet<string> participants,
        List<CombatLogEntry> bufferedEntries,
        Dictionary<string, HashSet<string>> playerSpells)
    {
        // Process source player
        if (!string.IsNullOrEmpty(parsed.SourceName))
        {
            var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(parsed.SourceName);
            var region = ExtractRegion(parsed.SourceName);
            
            if (!string.IsNullOrEmpty(playerName))
            {
                var cached = _playerCache.GetCached(playerName);
                if (cached != null)
                {
                    playersByKey.TryAdd(playerName, cached);
                    participants.Add(playerName);
                }
                else if (!string.IsNullOrWhiteSpace(realm))
                {
                    _playerCache.GetOrAddPending(playerName, realm);
                    if (!string.IsNullOrEmpty(region))
                    {
                        _playerRegions[playerName] = region;
                    }
                    participants.Add(playerName);
                }

                // Track spells
                if (!string.IsNullOrEmpty(parsed.SpellName))
                {
                    if (!playerSpells.TryGetValue(playerName, out var spells))
                    {
                        spells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        playerSpells[playerName] = spells;
                    }
                    spells.Add(parsed.SpellName);
                }
            }
        }

        // Process target player
        if (!string.IsNullOrEmpty(parsed.TargetName))
        {
            var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(parsed.TargetName);
            var region = ExtractRegion(parsed.TargetName);
            
            if (!string.IsNullOrEmpty(playerName))
            {
                var cached = _playerCache.GetCached(playerName);
                if (cached != null)
                {
                    playersByKey.TryAdd(playerName, cached);
                    participants.Add(playerName);
                }
                else if (!string.IsNullOrWhiteSpace(realm))
                {
                    _playerCache.GetOrAddPending(playerName, realm);
                    if (!string.IsNullOrEmpty(region))
                    {
                        _playerRegions[playerName] = region;
                    }
                    participants.Add(playerName);
                }
            }
        }

        // Create combat log entry if we have a source player
        if (!string.IsNullOrEmpty(parsed.SourceName) && playersByKey.TryGetValue(
                PlayerInfoExtractor.ParsePlayerName(parsed.SourceName).Name, out var source))
        {
            if (source.Id > 0)
            {
                Player? target = null;
                if (!string.IsNullOrEmpty(parsed.TargetName) && playersByKey.TryGetValue(
                        PlayerInfoExtractor.ParsePlayerName(parsed.TargetName).Name, out var targetPlayer))
                {
                    target = targetPlayer;
                }

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
        }
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

            if (player.Class == originalClass && player.Faction == originalFaction && player.Spec == originalSpec) continue;
            _playerCache.MarkForUpdate(player);
            logger.LogDebug("Marked player {PlayerName} for update: Class={Class}, Faction={Faction}, Spec={Spec}",
                player.Name, player.Class, player.Faction, player.Spec);
        }

        return Task.CompletedTask;
    }

    private async Task EnrichPlayersWithWowApiAsync(List<string> playerNamesToEnrich, CancellationToken ct)
    {
        var playersToEnrich = playerNamesToEnrich
            .Select(name => _playerCache.GetCached(name))
            .OfType<Player>()
            .Where(cached => string.IsNullOrWhiteSpace(cached.Class) || string.IsNullOrWhiteSpace(cached.Faction) || string.IsNullOrWhiteSpace(cached.Spec))
            .ToList();

        foreach (var player in playersToEnrich)
        {
            try
            {
                var region = _playerRegions.GetValueOrDefault(player.Name, "eu");
                var apiData = await wowApiService.GetPlayerDataAsync(player.Realm, player.Name, region, ct);

                if (apiData != null)
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

                    if (needsUpdate)
                    {
                        _playerCache.MarkForUpdate(player);
                        logger.LogDebug("Enriched player {PlayerName} from WoW API: Class={Class}, Faction={Faction}",
                            player.Name, player.Class, player.Faction);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to enrich player {PlayerName} from WoW API", player.Name);
            }
        }
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
        string arenaMatchId)
    {
        var uniqueHash = ComputeMatchHash(participants, start, end, arenaMatchId);
        
        // Check if match with this UniqueHash already exists
        var existingMatches = await matchRepo.ListAsync(m => m.UniqueHash == uniqueHash, ct);
        if (existingMatches.Count > 0)
        {
            logger.LogInformation("Match with UniqueHash {UniqueHash} already exists, skipping duplicate.", uniqueHash);
            return existingMatches[0]; // Return existing match
        }

        var match = new Match
        {
            CreatedOn = start ?? DateTime.UtcNow,
            MapName = "Nagrand Arena",
            ArenaZone = arenaZone,
            ArenaMatchId = arenaMatchId,
            GameMode = gameMode,
            Duration = start.HasValue && end.HasValue ? (long)(end.Value - start.Value).TotalSeconds : 0,
            IsRanked = true,
            UniqueHash = uniqueHash
        };
        match = await matchRepo.AddAsync(match, ct);

        logger.LogDebug("Persisted match {MatchId} with {ParticipantCount} participants.", match.Id,
            participants.Count);

        foreach (var e in entries)
        {
            e.MatchId = match.Id;
            await entryRepo.AddAsync(e, ct);
        }

        foreach (var name in participants)
        {
            if (!playersByKey.TryGetValue(name, out var player))
                continue;

            var matchSpec = string.Empty;
            if (playerSpells.TryGetValue(name, out var spells))
            {
                matchSpec = PlayerInfoExtractor.DetermineSpecForMatch(spells);
            }

            await resultRepo.AddAsync(new MatchResult
            {
                MatchId = match.Id,
                PlayerId = player.Id,
                Team = "Unknown",
                RatingBefore = 0,
                RatingAfter = 0,
                IsWinner = false,
                Spec = matchSpec
            }, ct);
        }

        return match;
    }

    private static string ComputeMatchHash(IEnumerable<string> playerKeys, DateTime? start, DateTime? end,
        string? arenaMatchId = null)
    {
        var baseStr = string.Join('|', playerKeys.OrderBy(x => x)) + $"|{start:O}|{end:O}|{arenaMatchId}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(baseStr)));
    }
}

