using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Core.Logs;

namespace PvpAnalytics.Application.Logs;

public class CombatLogIngestionService(
    IRepository<Player> playerRepo,
    IRepository<Match> matchRepo,
    IRepository<MatchResult> resultRepo,
    IRepository<CombatLogEntry> entryRepo,
    IWowApiService wowApiService,
    ILogger<CombatLogIngestionService> logger,
    ILoggerFactory loggerFactory)
    : ICombatLogIngestionService
{
    private readonly PlayerCache _playerCache = new PlayerCache(playerRepo);

    private readonly Dictionary<string, string>
        _playerRegions = new(StringComparer.OrdinalIgnoreCase); // Track region per player for WoW API

    /// <summary>
    /// Ingests combat-log text from the provided stream and persists arena matches, combat entries, players, and match results.
    /// Processes multiple matches: starts recording on ARENA_MATCH_START, stops on ZONE_CHANGE, saves all matches found.
    /// Automatically detects format (Traditional or Lua Table) and routes to appropriate parser.
    /// </summary>
    /// <param name="fileStream">A readable stream containing the combat log text (UTF-8 or BOM-detected).</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <remarks>
    /// Matches are detected by ARENA_MATCH_START events and finalized on ZONE_CHANGE events. All matches found in the file are persisted.
    /// </remarks>
    /// <returns>List of all persisted <see cref="Match"/> entities created from the stream.</returns>
    public async Task<List<Match>> IngestAsync(Stream fileStream, CancellationToken ct = default)
    {
        logger.LogInformation("Combat log ingestion started.");

        // Detect format and route to appropriate service
        var format = CombatLogFormatDetector.DetectFormat(fileStream);
        
        if (format == CombatLogFormat.LuaTable)
        {
            logger.LogInformation("Detected Lua table format, routing to Lua parser.");
            var luaLogger = loggerFactory.CreateLogger<LuaCombatLogIngestionService>();
            var luaService = new LuaCombatLogIngestionService(
                playerRepo,
                matchRepo,
                resultRepo,
                entryRepo,
                wowApiService,
                luaLogger);
            return await luaService.IngestAsync(fileStream, ct);
        }

        // Continue with traditional format parsing
        logger.LogInformation("Detected traditional format, using standard parser.");
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        // Track all persisted matches
        var allPersistedMatches = new List<Match>();

        // Current match buffers
        var playersByKey = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        var participants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bufferedEntries = new List<CombatLogEntry>();
        var playerSpells =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // Track spells per player
        DateTime? matchStart = null;
        DateTime? matchEnd = null;
        int? currentZoneId = null;
        string? currentArenaMatchId = null;
        bool matchInProgress = false;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue;

            var parsed = CombatLogParser.ParseLine(line);
            if (parsed == null) continue;

            switch (parsed.EventType)
            {
                case CombatLogEventTypes.ArenaMatchStart:
                {
                    matchStart = parsed.Timestamp;
                    currentArenaMatchId = parsed.ArenaMatchId;
                    matchInProgress = true;
                    participants.Clear();
                    bufferedEntries.Clear();
                    playerSpells.Clear();
                    playersByKey.Clear();

                    if (parsed.ZoneId.HasValue)
                    {
                        currentZoneId = parsed.ZoneId;
                        ArenaZoneIds.GetNameOrDefault(currentZoneId.Value);
                    }

                    logger.LogInformation("Arena match started: {ArenaMatchId} at {Timestamp}", currentArenaMatchId,
                        matchStart);
                    continue;
                }
                case CombatLogEventTypes.ZoneChange:
                {
                    if (matchInProgress && currentArenaMatchId != null)
                    {
                        matchEnd = parsed.Timestamp;

                        await _playerCache.BatchLookupAsync(ct);

                        foreach (var name in participants)
                        {
                            var cached = _playerCache.GetCached(name);
                            if (cached != null)
                            {
                                playersByKey.TryAdd(name, cached);
                            }
                        }

                        await UpdatePlayersFromSpellsAsync(playersByKey, playerSpells, ct);

                        var gameMode =
                            GameModeHelper.GetGameModeFromParticipantCount(participants.Count, currentArenaMatchId);
                        var arenaZone = currentZoneId.HasValue
                            ? ArenaZoneIds.GetArenaZone(currentZoneId.Value)
                            : ArenaZone.Unknown;
                        var mapName = ArenaZoneIds.GetDisplayName(arenaZone);
                        var persistedMatch = await FinalizeAndPersistAsync(arenaZone, matchStart, matchEnd,
                            participants, bufferedEntries, playersByKey, playerSpells, ct, gameMode,
                            currentArenaMatchId, mapName);
                        if (persistedMatch.Id > 0)
                        {
                            allPersistedMatches.Add(persistedMatch);
                            logger.LogInformation("Persisted match {MatchId} with arena match ID {ArenaMatchId}.",
                                persistedMatch.Id, currentArenaMatchId);
                        }

                        participants.Clear();
                        bufferedEntries.Clear();
                        playerSpells.Clear();
                        playersByKey.Clear();
                        matchStart = null;
                        matchEnd = null;
                        currentArenaMatchId = null;
                        matchInProgress = false;
                    }

                    if (parsed.ZoneId.HasValue)
                    {
                        currentZoneId = parsed.ZoneId;
                        _ = parsed.ZoneName ?? ArenaZoneIds.GetNameOrDefault(currentZoneId.Value);
                    }

                    continue;
                }
            }

            if (!matchInProgress)
                continue;

            var srcName = parsed.SourceName;
            var tgtName = parsed.TargetName;
            var spellName = parsed.SpellName;

            if (!string.IsNullOrEmpty(srcName) && !string.IsNullOrEmpty(spellName))
            {
                var (playerName, _) = PlayerInfoExtractor.ParsePlayerName(srcName);
                if (!string.IsNullOrEmpty(playerName))
                {
                    if (!playerSpells.TryGetValue(playerName, out var spells))
                    {
                        spells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        playerSpells[playerName] = spells;
                    }

                    spells.Add(spellName);
                }
            }

            if (!string.IsNullOrEmpty(srcName))
            {
                var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(srcName);
                var region = ExtractRegion(srcName);
                if (!string.IsNullOrEmpty(playerName))
                {
                    var cached = _playerCache.GetCached(playerName);
                    if (cached != null)
                    {
                        playersByKey.TryAdd(playerName, cached);
                        participants.Add(playerName);
                    }
                    else
                    {
                        // Add to pending creates if realm is present
                        if (!string.IsNullOrWhiteSpace(realm))
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
            }

            if (!string.IsNullOrEmpty(tgtName))
            {
                var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(tgtName);
                var region = ExtractRegion(tgtName);
                if (!string.IsNullOrEmpty(playerName))
                {
                    // Check cache first
                    var cached = _playerCache.GetCached(playerName);
                    if (cached != null)
                    {
                        playersByKey.TryAdd(playerName, cached);
                        participants.Add(playerName);
                    }
                    else
                    {
                        // Add to pending creates if realm is present
                        if (!string.IsNullOrWhiteSpace(realm))
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
            }

            matchEnd = parsed.Timestamp;

            var (sourceName, _) = !string.IsNullOrEmpty(srcName)
                ? PlayerInfoExtractor.ParsePlayerName(srcName)
                : (string.Empty, string.Empty);
            var (targetName, _) = !string.IsNullOrEmpty(tgtName)
                ? PlayerInfoExtractor.ParsePlayerName(tgtName)
                : (string.Empty, string.Empty);

            var source = !string.IsNullOrEmpty(sourceName) && playersByKey.TryGetValue(sourceName, out var src)
                ? src
                : null;
            var target = !string.IsNullOrEmpty(targetName) && playersByKey.TryGetValue(targetName, out var tgt)
                ? tgt
                : null;

            if (source is { Id: > 0 })
            {
                bufferedEntries.Add(new CombatLogEntry
                {
                    Timestamp = parsed.Timestamp,
                    SourcePlayerId = source.Id,
                    TargetPlayerId = target?.Id,
                    Ability = spellName ?? parsed.EventType,
                    DamageDone = parsed.Damage ?? 0,
                    HealingDone = parsed.Healing ?? 0,
                    CrowdControl = string.Empty
                });
            }
        }

        if (matchInProgress && currentArenaMatchId != null)
        {
            await _playerCache.BatchLookupAsync(ct);

            foreach (var name in participants)
            {
                var cached = _playerCache.GetCached(name);
                if (cached != null)
                {
                    playersByKey.TryAdd(name, cached);
                }
            }

            await UpdatePlayersFromSpellsAsync(playersByKey, playerSpells, ct);

            var gameMode = GameModeHelper.GetGameModeFromParticipantCount(participants.Count, currentArenaMatchId);
            var arenaZone = currentZoneId.HasValue
                ? ArenaZoneIds.GetArenaZone(currentZoneId.Value)
                : ArenaZone.Unknown;
            var mapName = ArenaZoneIds.GetDisplayName(arenaZone);
            var persistedMatch = await FinalizeAndPersistAsync(arenaZone, matchStart, matchEnd, participants,
                bufferedEntries, playersByKey, playerSpells, ct, gameMode, currentArenaMatchId, mapName);
            if (persistedMatch.Id > 0)
            {
                allPersistedMatches.Add(persistedMatch);
                logger.LogInformation("Persisted final match {MatchId} with arena match ID {ArenaMatchId}.",
                    persistedMatch.Id, currentArenaMatchId);
            }
        }

        // Capture pending player names before persisting (for enrichment)
        var pendingPlayerNames = _playerCache.GetPendingCreates().Keys.ToList();

        // Batch persist all pending creates and updates at file end
        await _playerCache.BatchPersistAsync(ct);

        // Enrich players with WoW API data if missing information
        // Pass the names of players that were just created so enrichment can find them in cache
        await EnrichPlayersWithWowApiAsync(pendingPlayerNames, ct);

        // Persist any updates from enrichment
        await _playerCache.BatchPersistAsync(ct);

        logger.LogInformation("Combat log ingestion completed. Persisted {MatchCount} match(es).",
            allPersistedMatches.Count);
        return allPersistedMatches;
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
        // Get players from cache that were just created and need enrichment
        // Note: Spec is not available from WoW API, so we only enrich for missing Class/Faction
        var playersToEnrich = playerNamesToEnrich
            .Select(name => _playerCache.GetCached(name))
            .OfType<Player>()
            .Where(cached => string.IsNullOrWhiteSpace(cached.Class) || string.IsNullOrWhiteSpace(cached.Faction))
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
        string arenaMatchId,
        string mapName)
    {
        var uniqueHash = ComputeMatchHash(participants, start, end, arenaMatchId);

        var match = new Match
        {
            CreatedOn = start ?? DateTime.UtcNow,
            ArenaZone = arenaZone,
            MapName = mapName,
            ArenaMatchId = arenaMatchId,
            GameMode = gameMode,
            Duration = start.HasValue && end.HasValue ? (long)(end.Value - start.Value).TotalSeconds : 0,
            IsRanked = true,
            UniqueHash = uniqueHash
        };

        try
        {
            match = await matchRepo.AddAsync(match, ct);
            logger.LogInformation("Persisted new match {MatchId} with UniqueHash {UniqueHash} and {ParticipantCount} participants.", 
                match.Id, uniqueHash, participants.Count);
        }
        catch (Exception ex)
        {
            // Check if this is a unique constraint violation (PostgreSQL error code 23505)
            var isUniqueConstraintViolation = ex.GetType().FullName?.Contains("DbUpdateException") == true &&
                                              ex.InnerException?.GetType().FullName?.Contains("PostgresException") == true &&
                                              (ex.InnerException.GetType().GetProperty("SqlState")?.GetValue(ex.InnerException)?.ToString() == "23505" ||
                                               ex.Message.Contains("23505") ||
                                               ex.Message.Contains("duplicate key value") ||
                                               ex.Message.Contains("IX_Matches_UniqueHash"));

            if (isUniqueConstraintViolation)
            {
                // Unique constraint violation - match already exists
                logger.LogInformation("Match with UniqueHash {UniqueHash} already exists in database, returning existing match.", uniqueHash);
                
                // Re-query to get the existing match
                var existingMatches = await matchRepo.ListAsync(m => m.UniqueHash == uniqueHash, ct);
                if (existingMatches.Count > 0)
                {
                    return existingMatches[0]; // Return existing match (skip creating entries/results for duplicate)
                }
            }
            
            // If not a unique constraint violation or query failed, re-throw
            throw;
        }

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