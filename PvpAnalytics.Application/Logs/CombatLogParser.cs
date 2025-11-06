using System.Globalization;
using PvpAnalytics.Core.Logs;

namespace PvpAnalytics.Application.Logs;

public class CombatLogParser
{
    private static readonly string[] TimestampFormats = { "M/d/yyyy H:mm:ss.ffff", "M/d H:mm:ss.fff", "M/d H:mm:ss" };

    public ParsedCombatLogEvent? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        // Split timestamp and csv by two spaces
        var parts = line.Split(new[] { "  " }, 2, StringSplitOptions.None);
        if (parts.Length != 2) return null;

        if (!TryParseTimestamp(parts[0], out var ts)) return null;

        var fields = parts[1].Split(',');
        if (fields.Length == 0) return null;
        var evt = fields[CombatLogFieldMappings.Common.Event].Trim();

        if (evt == CombatLogEventTypes.ZoneChange)
        {
            var zoneId = ParseInt(SafeField(fields, CombatLogFieldMappings.ZoneChange.ZoneId));
            var zoneName = SafeField(fields, CombatLogFieldMappings.ZoneChange.ZoneName);
            return new ParsedCombatLogEvent
            {
                Timestamp = ts,
                EventType = evt,
                ZoneId = zoneId,
                ZoneName = zoneName
            };
        }

        // Common combat events
        var sourceGuid = SafeField(fields, CombatLogFieldMappings.Common.SourceGuid);
        var sourceName = SafeField(fields, CombatLogFieldMappings.Common.SourceName);
        var targetGuid = SafeField(fields, CombatLogFieldMappings.Common.TargetGuid);
        var targetName = SafeField(fields, CombatLogFieldMappings.Common.TargetName);
        var spellId = ParseInt(SafeField(fields, CombatLogFieldMappings.Common.SpellId));
        var spellName = SafeField(fields, CombatLogFieldMappings.Common.SpellName);

        int? damage = null, healing = null, absorbed = null;
        switch (evt)
        {
            case CombatLogEventTypes.SwingDamage:
                damage = ParseInt(SafeField(fields, CombatLogFieldMappings.SwingDamage.Amount));
                break;
            case CombatLogEventTypes.SpellDamage:
            case CombatLogEventTypes.RangeDamage:
                damage = ParseInt(SafeField(fields, CombatLogFieldMappings.SpellDamage.Amount));
                break;
            case CombatLogEventTypes.SpellHeal:
            case CombatLogEventTypes.SpellPeriodicHeal:
                healing = ParseInt(SafeField(fields, CombatLogFieldMappings.SpellHeal.Amount));
                break;
            case CombatLogEventTypes.SpellAbsorbed:
                // Read absorbed amount; null if missing or unparsable
                absorbed = ParseInt(SafeField(fields, CombatLogFieldMappings.SpellAbsorbed.Amount));
                break;
        }

        return new ParsedCombatLogEvent
        {
            Timestamp = ts,
            EventType = evt,
            SourceGuid = sourceGuid,
            SourceName = TrimQuotes(sourceName),
            TargetGuid = targetGuid,
            TargetName = TrimQuotes(targetName),
            SpellId = spellId,
            SpellName = TrimQuotes(spellName),
            Damage = damage,
            Healing = healing,
            Absorbed = absorbed
        };
    }

    public bool IsArenaZone(int zoneId) => ArenaZoneIds.IsArena(zoneId);

    private static bool TryParseTimestamp(string input, out DateTime timestamp)
    {
        // Try with date first; then without year
        if (DateTime.TryParseExact(input, TimestampFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out timestamp))
            return true;
        // Fallback: let DateTime parser try
        return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out timestamp);
    }

    private static string SafeField(string[] arr, int idx) => idx < arr.Length ? arr[idx] : string.Empty;
    private static int? ParseInt(string s) => int.TryParse(TrimQuotes(s), out var v) ? v : null;
    private static string TrimQuotes(string s) => s.Trim(' ', '"');
}


