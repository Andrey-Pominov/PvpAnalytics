using System.Text;
using Microsoft.Extensions.Logging;
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
    ILogger<CombatLogIngestionService> logger)
    : ICombatLogIngestionService
{
    private readonly ILogger<CombatLogIngestionService> _logger = logger;
    private readonly PlayerCache _playerCache = new PlayerCache(playerRepo);
    private readonly Dictionary<string, string> _playerRegions = new(StringComparer.OrdinalIgnoreCase); // Track region per player for WoW API
    /// <summary>
    /// Ingests combat-log text from the provided stream and persists arena matches, combat entries, players, and match results.
    /// Processes multiple matches: starts recording on ARENA_MATCH_START, stops on ZONE_CHANGE, saves all matches found.
    /// </summary>
    /// <param name="fileStream">A readable stream containing the combat log text (UTF-8 or BOM-detected).</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <remarks>
    /// Matches are detected by ARENA_MATCH_START events and finalized on ZONE_CHANGE events. All matches found in the file are persisted.
    /// </remarks>
    /// <returns>List of all persisted <see cref="Match"/> entities created from the stream.</returns>
    public async Task<List<Match>> IngestAsync(Stream fileStream, CancellationToken ct = default)
    {
        _logger.LogInformation("Combat log ingestion started.");
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var parser = new CombatLogParser();

        // Track all persisted matches
        var allPersistedMatches = new List<Match>();
        
        // Current match buffers
        var playersByKey = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        var participants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bufferedEntries = new List<CombatLogEntry>();
        var playerSpells = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // Track spells per player
        DateTime? matchStart = null;
        DateTime? matchEnd = null;
        string? currentZoneName = null;
        int? currentZoneId = null;
        string? currentArenaMatchId = null;
        bool matchInProgress = false;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue; // header lines

            var parsed = CombatLogParser.ParseLine(line);
            if (parsed == null) continue;

            // Handle ARENA_MATCH_START - start recording a new match
            if (parsed.EventType == CombatLogEventTypes.ArenaMatchStart)
            {
                // Initialize new match buffers
                matchStart = parsed.Timestamp;
                currentArenaMatchId = parsed.ArenaMatchId;
                matchInProgress = true;
                participants.Clear();
                bufferedEntries.Clear();
                playerSpells.Clear(); // Reset spell tracking for new match
                playersByKey.Clear(); // Clear match-specific player cache
                
                if (parsed.ZoneId.HasValue)
                {
                    currentZoneId = parsed.ZoneId;
                    currentZoneName = ArenaZoneIds.GetNameOrDefault(currentZoneId.Value);
                }
                
                _logger.LogInformation("Arena match started: {ArenaMatchId} at {Timestamp}", currentArenaMatchId, matchStart);
                continue;
            }

            // Handle ZONE_CHANGE - arena ended, finalize current match and stop recording
            if (parsed.EventType == CombatLogEventTypes.ZoneChange)
            {
                // Finalize existing arena match if one is in progress
                if (matchInProgress && currentArenaMatchId != null)
                {
                    matchEnd = parsed.Timestamp;
                    
                    // Batch lookup players from database before finalizing
                    await _playerCache.BatchLookupAsync(ct);
                    
                    // Populate playersByKey from cache
                    foreach (var name in participants)
                    {
                        var cached = _playerCache.GetCached(name);
                        if (cached != null && !playersByKey.ContainsKey(name))
                        {
                            playersByKey[name] = cached;
                        }
                    }
                    
                    // Update players with discovered information from spells before finalizing
                    await UpdatePlayersFromSpellsAsync(playersByKey, playerSpells, ct);
                    
                    var gameMode = GameModeHelper.GetGameModeFromParticipantCount(participants.Count, currentArenaMatchId);
                    var arenaZone = currentZoneId.HasValue 
                        ? ArenaZoneIds.GetArenaZone(currentZoneId.Value) 
                        : ArenaZone.Unknown;
                    var persistedMatch = await FinalizeAndPersistAsync(arenaZone, matchStart, matchEnd, participants, bufferedEntries, playersByKey, playerSpells, ct, gameMode, currentArenaMatchId);
                    if (persistedMatch.Id > 0)
                    {
                        allPersistedMatches.Add(persistedMatch);
                        _logger.LogInformation("Persisted match {MatchId} with arena match ID {ArenaMatchId}.", persistedMatch.Id, currentArenaMatchId);
                    }

                    // Reset match-specific buffers
                    participants.Clear();
                    bufferedEntries.Clear();
                    playerSpells.Clear();
                    playersByKey.Clear();
                    _playerCache.ClearPending();
                    matchStart = null;
                    matchEnd = null;
                    currentArenaMatchId = null;
                    matchInProgress = false;
                }

                // Update zone info but don't process events until next ARENA_MATCH_START
                if (parsed.ZoneId.HasValue)
                {
                    currentZoneId = parsed.ZoneId;
                    currentZoneName = parsed.ZoneName ?? ArenaZoneIds.GetNameOrDefault(currentZoneId.Value);
                }

                continue; // Skip all events until next ARENA_MATCH_START
            }

            // Only process combat events when a match is in progress
            if (!matchInProgress)
                continue; // Skip events between ZONE_CHANGE and next ARENA_MATCH_START

            // Track spells for player attribute detection
            var srcName = parsed.SourceName;
            var tgtName = parsed.TargetName;
            var spellName = parsed.SpellName;

            // Track spells from source player (for class/spec/faction detection)
            // Track from all event types: SPELL_AURA_APPLIED, SPELL_CAST_SUCCESS, SPELL_DAMAGE, SPELL_HEAL, etc.
            // The source is always the caster, so tracking source spells identifies the player's class/spec/faction
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

            // Cache players during parsing (will be batch looked up and created later)
            if (!string.IsNullOrEmpty(srcName))
            {
                var (playerName, realm) = PlayerInfoExtractor.ParsePlayerName(srcName);
                var region = ExtractRegion(srcName);
                if (!string.IsNullOrEmpty(playerName))
                {
                    // Check cache first
                    var cached = _playerCache.GetCached(playerName);
                    if (cached != null)
                    {
                        if (!playersByKey.ContainsKey(playerName))
                        {
                            playersByKey[playerName] = cached;
                        }
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
                        if (!playersByKey.ContainsKey(playerName))
                        {
                            playersByKey[playerName] = cached;
                        }
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

            var (sourceName, _) = !string.IsNullOrEmpty(srcName) ? PlayerInfoExtractor.ParsePlayerName(srcName) : (string.Empty, string.Empty);
            var (targetName, _) = !string.IsNullOrEmpty(tgtName) ? PlayerInfoExtractor.ParsePlayerName(tgtName) : (string.Empty, string.Empty);

            var source = !string.IsNullOrEmpty(sourceName) && playersByKey.TryGetValue(sourceName, out var src) ? src : null;
            var target = !string.IsNullOrEmpty(targetName) && playersByKey.TryGetValue(targetName, out var tgt) ? tgt : null;

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

        // EOF: finalize if match still in progress
        if (matchInProgress && currentArenaMatchId != null)
        {
            // Batch lookup players from database before finalizing
            await _playerCache.BatchLookupAsync(ct);
            
            // Populate playersByKey from cache
            foreach (var name in participants)
            {
                var cached = _playerCache.GetCached(name);
                if (cached != null && !playersByKey.ContainsKey(name))
                {
                    playersByKey[name] = cached;
                }
            }
            
            // Update players with discovered information from spells before finalizing
            await UpdatePlayersFromSpellsAsync(playersByKey, playerSpells, ct);
            
            var gameMode = GameModeHelper.GetGameModeFromParticipantCount(participants.Count, currentArenaMatchId);
            var arenaZone = currentZoneId.HasValue 
                ? ArenaZoneIds.GetArenaZone(currentZoneId.Value) 
                : ArenaZone.Unknown;
            var persistedMatch = await FinalizeAndPersistAsync(arenaZone, matchStart, matchEnd, participants, bufferedEntries, playersByKey, playerSpells, ct, gameMode, currentArenaMatchId);
            if (persistedMatch.Id > 0)
            {
                allPersistedMatches.Add(persistedMatch);
                _logger.LogInformation("Persisted final match {MatchId} with arena match ID {ArenaMatchId}.", persistedMatch.Id, currentArenaMatchId);
            }
        }

        // Batch persist all pending creates and updates at file end
        await _playerCache.BatchPersistAsync(ct);
        
        // Enrich players with WoW API data if missing information
        await EnrichPlayersWithWowApiAsync(ct);

        _logger.LogInformation("Combat log ingestion completed. Persisted {MatchCount} match(es).", allPersistedMatches.Count);
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
                return suffix.Substring(1).ToLowerInvariant(); // Remove leading dash and lowercase
            }
        }
        return "eu"; // Default to EU
    }

    private Task UpdatePlayersFromSpellsAsync(
        Dictionary<string, Player> playersByKey,
        Dictionary<string, HashSet<string>> playerSpells,
        CancellationToken ct)
    {
        foreach (var kvp in playerSpells)
        {
            var playerName = kvp.Key;
            var spells = kvp.Value;

            if (!playersByKey.TryGetValue(playerName, out var player) || spells.Count == 0)
                continue;

            // Check if player needs updates
            var originalClass = player.Class;
            var originalFaction = player.Faction;

            // Update player with spell analysis
            PlayerInfoExtractor.UpdatePlayerFromSpells(player, spells);

            // Check if any attributes were updated
            if (player.Class != originalClass || player.Faction != originalFaction)
            {
                // Mark for batch update
                _playerCache.MarkForUpdate(player);
                _logger.LogDebug("Marked player {PlayerName} for update: Class={Class}, Faction={Faction}", 
                    player.Name, player.Class, player.Faction);
            }
        }
        return Task.CompletedTask;
    }

    private async Task EnrichPlayersWithWowApiAsync(CancellationToken ct)
    {
        var pendingCreates = _playerCache.GetPendingCreates();
        var playersToEnrich = new List<Player>();

        // Collect players that need enrichment
        foreach (var kvp in pendingCreates)
        {
            var pending = kvp.Value;
            var cached = _playerCache.GetCached(pending.Name);
            if (cached != null && (
                string.IsNullOrWhiteSpace(cached.Class) ||
                string.IsNullOrWhiteSpace(cached.Faction)))
            {
                playersToEnrich.Add(cached);
            }
        }

        // Also check already cached players that might need enrichment
        // (This would require exposing cache contents, but for now we'll focus on pending creates)

        // Enrich players with WoW API
        foreach (var player in playersToEnrich)
        {
            try
            {
                var region = _playerRegions.TryGetValue(player.Name, out var r) ? r : "eu";
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
                        _logger.LogDebug("Enriched player {PlayerName} from WoW API: Class={Class}, Faction={Faction}",
                            player.Name, player.Class, player.Faction);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich player {PlayerName} from WoW API", player.Name);
            }
        }
    }

    private async Task<Match> FinalizeAndPersistAsync(
        Core.Enum.ArenaZone arenaZone,
        DateTime? start,
        DateTime? end,
        HashSet<string> participants,
        List<CombatLogEntry> entries,
        Dictionary<string, Player> playersByKey,
        Dictionary<string, HashSet<string>> playerSpells,
        CancellationToken ct,
        Core.Enum.GameMode gameMode,
        string arenaMatchId)
    {
        var match = new Match
        {
            CreatedOn = start ?? DateTime.UtcNow,
            ArenaZone = arenaZone,
            ArenaMatchId = arenaMatchId,
            GameMode = gameMode,
            Duration = start.HasValue && end.HasValue ? (long)(end.Value - start.Value).TotalSeconds : 0,
            IsRanked = true,
            UniqueHash = ComputeMatchHash(participants, start, end, arenaMatchId)
        };
        match = await matchRepo.AddAsync(match, ct);

        _logger.LogDebug("Persisted match {MatchId} with {ParticipantCount} participants.", match.Id, participants.Count);

        foreach (var e in entries)
        {
            e.MatchId = match.Id;
            await entryRepo.AddAsync(e, ct);
        }

        foreach (var name in participants)
        {
            if (!playersByKey.TryGetValue(name, out var player))
                continue;

            // Determine spec for this match from spell analysis
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


    

    private static string ComputeMatchHash(IEnumerable<string> playerKeys, DateTime? start, DateTime? end, string? arenaMatchId = null)
    {
        var baseStr = string.Join('|', playerKeys.OrderBy(x => x)) + $"|{start:O}|{end:O}|{arenaMatchId}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(baseStr)));
    }

}

