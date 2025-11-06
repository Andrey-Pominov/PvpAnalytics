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

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue; // header lines

            var parsed = parser.ParseLine(line);
            if (parsed == null) continue;

            // Handle ZONE_CHANGE
            if (parsed.EventType == CombatLogEventTypes.ZoneChange && parsed.ZoneId.HasValue)
            {
                // Finalize existing arena match if any
                if (arenaActive && currentZoneId.HasValue)
                {
                    matchEnd = parsed.Timestamp;
                    lastPersistedMatch = await FinalizeAndPersistAsync(currentZoneName ?? "Unknown", matchStart, matchEnd, participants, bufferedEntries, playersByKey, ct, parser.GetGameMode(currentZoneId.Value));
                }

                // Reset buffers
                participants.Clear();
                bufferedEntries.Clear();
                matchStart = null;
                matchEnd = null;

                currentZoneId = parsed.ZoneId;
                currentZoneName = parsed.ZoneName ?? ArenaZoneIds.GetNameOrDefault(currentZoneId.Value);
                arenaActive = currentZoneId.HasValue && parser.IsArenaZone(currentZoneId.Value);
                if (arenaActive && currentZoneId.HasValue)
                {
                    matchStart = parsed.Timestamp;
                }

                continue;
            }

            // Always auto-create players on sight for any parsed combat event
            var srcName = parsed.SourceName;
            var tgtName = parsed.TargetName;
            if (!string.IsNullOrEmpty(srcName))
            {
                var s = await GetOrCreatePlayerAsync(NormalizePlayerName(srcName), ct);
                playersByKey[s.Name] = s;
                participants.Add(s.Name);
            }
            if (!string.IsNullOrEmpty(tgtName))
            {
                var t = await GetOrCreatePlayerAsync(NormalizePlayerName(tgtName!), ct);
                playersByKey[t.Name] = t;
                participants.Add(t.Name);
            }

            if (!arenaActive)
                continue; // Only record combat entries during arena

            matchStart ??= parsed.Timestamp;
            matchEnd = parsed.Timestamp;

            var source = !string.IsNullOrEmpty(srcName) ? playersByKey[NormalizePlayerName(srcName)] : null;
            Player? target = !string.IsNullOrEmpty(tgtName) ? playersByKey[NormalizePlayerName(tgtName!)] : null;

            bufferedEntries.Add(new CombatLogEntry
            {
                Timestamp = parsed.Timestamp,
                SourcePlayerId = source?.Id ?? 0,
                TargetPlayerId = target?.Id,
                Ability = parsed.SpellId.HasValue ? (parsed.SpellName ?? parsed.EventType) : (parsed.SpellName ?? parsed.EventType),
                DamageDone = parsed.Damage ?? 0,
                HealingDone = parsed.Healing ?? 0,
                CrowdControl = string.Empty
            });
        }

        // EOF: finalize if arena match still active
        if (arenaActive && currentZoneId.HasValue)
        {
            lastPersistedMatch = await FinalizeAndPersistAsync(currentZoneName ?? "Unknown", matchStart, matchEnd, participants, bufferedEntries, playersByKey, ct, parser.GetGameMode(currentZoneId.Value));
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


