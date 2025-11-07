using System.Text;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Core.Logs;

namespace PvpAnalytics.Application.Logs;

public class CombatLogIngestionService(
    IRepository<Player> playerRepo,
    IRepository<Match> matchRepo,
    IRepository<MatchResult> resultRepo,
    IRepository<CombatLogEntry> entryRepo)
    : ICombatLogIngestionService
{
    /// <summary>
    /// Ingests combat-log text from the provided stream and persists arena matches, combat entries, players, and match results.
    /// </summary>
    /// <param name="fileStream">A readable stream containing the combat log text (UTF-8 or BOM-detected).</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <remarks>
    /// Only arena-zone combat entries are recorded; players seen outside arenas are still created and tracked. Matches are finalized on zone changes and at end-of-file.
    /// </remarks>
    /// <returns>The last persisted <see cref="Match"/> created from the stream, or a synthesized <see cref="Match"/> with <c>Id = 0</c> if no match was persisted.</returns>
    public async Task<Match> IngestAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var parser = new CombatLogParser();

        // Current match buffers
        var playersByKey = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        var participants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bufferedEntries = new List<CombatLogEntry>();
        DateTime? matchStart = null;
        DateTime? matchEnd = null;
        string? currentZoneName = null;
        int? currentZoneId = null;
        bool arenaActive = false;

        Match? lastPersistedMatch = null;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue; // header lines

            var parsed = CombatLogParser.ParseLine(line);
            if (parsed == null) continue;

            // Handle ZONE_CHANGE
            if (parsed.EventType == CombatLogEventTypes.ZoneChange && parsed.ZoneId.HasValue)
            {
                // Finalize existing arena match if any
                if (arenaActive && currentZoneId.HasValue)
                {
                    matchEnd = parsed.Timestamp;
                    var gameMode = GameModeHelper.GetGameModeFromParticipantCount(participants.Count);
                    lastPersistedMatch = await FinalizeAndPersistAsync(currentZoneName ?? "Unknown", matchStart, matchEnd, participants, bufferedEntries, playersByKey, ct, gameMode);
                }

                // Reset buffers
                participants.Clear();
                bufferedEntries.Clear();
                matchStart = null;
                matchEnd = null;

                currentZoneId = parsed.ZoneId;
                currentZoneName = parsed.ZoneName ?? ArenaZoneIds.GetNameOrDefault(currentZoneId.Value);
                arenaActive = currentZoneId.HasValue && CombatLogParser.IsArenaZone(currentZoneId.Value);
                if (arenaActive && currentZoneId.HasValue)
                {
                    matchStart = parsed.Timestamp;
                }

                continue;
            }

            // Always auto-create players on sight for any parsed combat event, with cache
            var srcName = parsed.SourceName;
            var tgtName = parsed.TargetName;
            if (!string.IsNullOrEmpty(srcName))
            {
                var normSrc = NormalizePlayerName(srcName);
                if (!playersByKey.TryGetValue(normSrc, out var s))
                {
                    s = await GetOrCreatePlayerAsync(normSrc, ct);
                    playersByKey[s.Name] = s;
                }
                participants.Add(s.Name);
            }
            if (!string.IsNullOrEmpty(tgtName))
            {
                var normTgt = NormalizePlayerName(tgtName);
                if (!playersByKey.TryGetValue(normTgt, out var t))
                {
                    t = await GetOrCreatePlayerAsync(normTgt, ct);
                    playersByKey[t.Name] = t;
                }
                participants.Add(t.Name);
            }

            if (!arenaActive)
                continue; // Only record combat entries during arena

            matchStart ??= parsed.Timestamp;
            matchEnd = parsed.Timestamp;

            var source = !string.IsNullOrEmpty(srcName) ? playersByKey[NormalizePlayerName(srcName)] : null;
            var target = !string.IsNullOrEmpty(tgtName) ? playersByKey[NormalizePlayerName(tgtName)] : null;

            if (source is { Id: > 0 })
            {
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

        // EOF: finalize if arena match still active
        if (arenaActive && currentZoneId.HasValue)
        {
            var gameMode = GameModeHelper.GetGameModeFromParticipantCount(participants.Count);
            lastPersistedMatch = await FinalizeAndPersistAsync(currentZoneName ?? "Unknown", matchStart, matchEnd, participants, bufferedEntries, playersByKey, ct, gameMode);
        }

        // Return the last match persisted (or a dummy if none)
        return lastPersistedMatch ?? new Match
        {
            Id = 0,
            MapName = currentZoneName ?? "Unknown",
            CreatedOn = matchStart ?? DateTime.UtcNow,
            Duration = matchStart.HasValue && matchEnd.HasValue ? (long)(matchEnd.Value - matchStart.Value).TotalSeconds : 0,
            GameMode = Core.Enum.GameMode.TwoVsTwo,
            IsRanked = false,
            UniqueHash = ComputeMatchHash(playersByKey.Keys, matchStart, matchEnd)
        };

        async Task<Player> GetOrCreatePlayerAsync(string name, CancellationToken token)
        {
            var existing = await playerRepo.ListAsync(p => p.Name == name, token);
            var player = existing.FirstOrDefault();
            if (player != null) return player;
            player = new Player { Name = name, Realm = string.Empty, Class = string.Empty, Spec = string.Empty, Faction = string.Empty };
            return await playerRepo.AddAsync(player, token);
        }
    }

    private async Task<Match> FinalizeAndPersistAsync(
        string zone,
        DateTime? start,
        DateTime? end,
        HashSet<string> participants,
        List<CombatLogEntry> entries,
        Dictionary<string, Player> playersByKey,
        CancellationToken ct,
        Core.Enum.GameMode gameMode)
    {
        var match = new Match
        {
            CreatedOn = start ?? DateTime.UtcNow,
            MapName = zone,
            GameMode = gameMode,
            Duration = start.HasValue && end.HasValue ? (long)(end.Value - start.Value).TotalSeconds : 0,
            IsRanked = true,
            UniqueHash = ComputeMatchHash(participants, start, end)
        };
        match = await matchRepo.AddAsync(match, ct);

        foreach (var e in entries)
        {
            e.MatchId = match.Id;
            await entryRepo.AddAsync(e, ct);
        }

        foreach (var name in participants)
        {
            var player = playersByKey[name];
            await resultRepo.AddAsync(new MatchResult
            {
                MatchId = match.Id,
                PlayerId = player.Id,
                Team = "Unknown",
                RatingBefore = 0,
                RatingAfter = 0,
                IsWinner = false
            }, ct);
        }

        return match;
    }

    private static string NormalizePlayerName(string name)
    {
        // Strip realm suffix if present: Name-Realm
        var trimmed = name.Trim('"');
        var dash = trimmed.IndexOf('-');
        return dash > 0 ? trimmed[..dash] : trimmed;
    }

    

    private static string ComputeMatchHash(IEnumerable<string> playerKeys, DateTime? start, DateTime? end)
    {
        var baseStr = string.Join('|', playerKeys.OrderBy(x => x)) + $"|{start:O}|{end:O}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(baseStr)));
    }

}

