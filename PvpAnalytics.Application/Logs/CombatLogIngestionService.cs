using System.Globalization;
using System.Text;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

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

        // Current match buffers
        var playersByKey = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        var participants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bufferedEntries = new List<CombatLogEntry>();
        DateTime? matchStart = null;
        DateTime? matchEnd = null;
        string? currentZone = null;
        bool arenaActive = false;

        Match? lastPersistedMatch = null;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue; // header lines

            // Handle ZONE_CHANGE lines specially
            if (TryParseZoneChange(line, out var zTs, out var zoneName))
            {
                // Finalize existing arena match if any
                if (arenaActive && currentZone != null)
                {
                    matchEnd = zTs;
                    lastPersistedMatch = await FinalizeAndPersistAsync(currentZone, matchStart, matchEnd, participants, bufferedEntries, playersByKey, ct);
                }

                // Reset buffers
                participants.Clear();
                bufferedEntries.Clear();
                matchStart = null;
                matchEnd = null;

                currentZone = zoneName;
                arenaActive = IsArenaZone(zoneName);
                if (arenaActive)
                {
                    matchStart = zTs;
                }

                continue;
            }

            if (!TryParseLine(line, out var ts, out var evt, out var sourceName, out var targetName, out var ability, out var damage, out var healing, out var cc))
                continue;

            // Always auto-create players on sight
            if (!string.IsNullOrEmpty(sourceName))
            {
                var s = await GetOrCreatePlayerAsync(NormalizePlayerName(sourceName), ct);
                playersByKey[s.Name] = s;
                participants.Add(s.Name);
            }
            if (!string.IsNullOrEmpty(targetName))
            {
                var t = await GetOrCreatePlayerAsync(NormalizePlayerName(targetName!), ct);
                playersByKey[t.Name] = t;
                participants.Add(t.Name);
            }

            if (!arenaActive)
                continue; // Only record combat entries during arena

            matchStart ??= ts;
            matchEnd = ts;

            var source = !string.IsNullOrEmpty(sourceName) ? playersByKey[NormalizePlayerName(sourceName)] : null;
            Player? target = !string.IsNullOrEmpty(targetName) ? playersByKey[NormalizePlayerName(targetName!)] : null;

            bufferedEntries.Add(new CombatLogEntry
            {
                Timestamp = ts,
                SourcePlayerId = source?.Id ?? 0,
                TargetPlayerId = target?.Id,
                Ability = ability ?? evt,
                DamageDone = damage,
                HealingDone = healing,
                CrowdControl = cc ?? string.Empty
            });
        }

        // EOF: finalize if arena match still active
        if (arenaActive && currentZone != null)
        {
            lastPersistedMatch = await FinalizeAndPersistAsync(currentZone, matchStart, matchEnd, participants, bufferedEntries, playersByKey, ct);
        }

        // Return the last match persisted (or a dummy if none)
        return lastPersistedMatch ?? new Match
        {
            Id = 0,
            MapName = currentZone ?? "Unknown",
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
        CancellationToken ct)
    {
        var match = new Match
        {
            CreatedOn = start ?? DateTime.UtcNow,
            MapName = zone,
            GameMode = TryMapGameMode(zone),
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

    private static bool TryParseZoneChange(string line, out DateTime timestamp, out string zone)
    {
        timestamp = DateTime.MinValue;
        zone = string.Empty;
        var parts = line.Split(new[] { "  " }, 2, StringSplitOptions.None);
        if (parts.Length != 2) return false;
        if (!DateTime.TryParseExact(parts[0], new[] { "M/d H:mm:ss.fff", "M/d H:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
            return false;
        var csv = parts[1];
        var fields = csv.Split(',');
        if (fields.Length < 2) return false;
        var evt = fields[0].Trim();
        if (!string.Equals(evt, "ZONE_CHANGE", StringComparison.OrdinalIgnoreCase)) return false;
        // Zone name usually quoted in next field
        zone = fields.Length > 1 ? fields[1].Trim(' ', '"') : string.Empty;
        return !string.IsNullOrEmpty(zone);
    }

    private static string NormalizePlayerName(string name)
    {
        // Strip realm suffix if present: Name-Realm
        var trimmed = name.Trim('"');
        var dash = trimmed.IndexOf('-');
        return dash > 0 ? trimmed[..dash] : trimmed;
    }

    private static bool IsArenaZone(string zone)
    {
        return ArenaZones.Contains(zone);
    }

    private static Core.Enum.GameMode TryMapGameMode(string zone)
    {
        // Without team counts, default to 2v2. Could be improved later.
        return Core.Enum.GameMode.TwoVsTwo;
    }

    private static bool TryParseLine(
        string line,
        out DateTime timestamp,
        out string @event,
        out string sourceName,
        out string? targetName,
        out string? ability,
        out int damage,
        out int healing,
        out string? crowdControl)
    {
        timestamp = DateTime.MinValue;
        @event = string.Empty;
        sourceName = string.Empty;
        targetName = null;
        ability = null;
        damage = 0;
        healing = 0;
        crowdControl = null;

        // Expected: "M/D H:mm:ss.fff  EVENT,comma,separated,fields,..."
        var parts = line.Split(new[] { "  " }, 2, StringSplitOptions.None);
        if (parts.Length != 2) return false;

        if (!DateTime.TryParseExact(parts[0], new[] { "M/d H:mm:ss.fff", "M/d H:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
            return false;

        var csv = parts[1];
        var fields = csv.Split(',');
        if (fields.Length < 3) return false;

        @event = fields[0].Trim();

        // WoW logs typically: event, sourceGUID, sourceName, sourceFlags, ..., targetGUID, targetName, ...
        sourceName = SafeField(fields, 2);
        targetName = fields.Length > 6 ? SafeField(fields, 6) : null;

        switch (@event)
        {
            case var e when e.StartsWith("SWING_"):
                ability = "SWING";
                if (fields.Length > 9 && int.TryParse(SafeField(fields, 9), out var swingDmg)) damage = swingDmg;
                break;
            case var e when e.StartsWith("RANGE_"):
            case var e2 when e2.StartsWith("SPELL_"):
                // SPELL_* / RANGE_* have spellId, spellName around fields[9]/[10]
                ability = fields.Length > 10 ? SafeField(fields, 10) : null;
                // damage at typical index 12 or later depending on event
                if (@event.Contains("_DAMAGE") && fields.Length > 12 && int.TryParse(SafeField(fields, 12), out var dmg)) damage = dmg;
                if (@event.Contains("_HEAL") && fields.Length > 12 && int.TryParse(SafeField(fields, 12), out var heal)) healing = heal;
                if (@event == "SPELL_AURA_APPLIED") crowdControl = ability; // naive CC capture
                break;
        }

        return true;

        static string SafeField(string[] arr, int idx) => idx < arr.Length ? arr[idx].Trim(' ', '"') : string.Empty;
    }

    private static string ComputeMatchHash(IEnumerable<string> playerKeys, DateTime? start, DateTime? end)
    {
        var baseStr = string.Join('|', playerKeys.OrderBy(x => x)) + $"|{start:O}|{end:O}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(baseStr)));
    }

    private static readonly HashSet<string> ArenaZones = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nagrand Arena",
        "Blade's Edge Arena",
        "Dalaran Arena",
        "Ruins of Lordaeron",
        "The Tiger's Peak",
        "Mugambala",
        "Ashamane's Fall",
        "Black Rook Hold Arena",
        "Empyrean Domain",
        "Maldraxxus Coliseum",
        "Nokhudon Proving Grounds",
        "Hook Point",
        "Tol'viron Arena"
    };
}


