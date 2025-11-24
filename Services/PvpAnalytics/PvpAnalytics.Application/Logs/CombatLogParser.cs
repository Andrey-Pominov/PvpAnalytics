using System.Globalization;
using PvpAnalytics.Core.Logs;

namespace PvpAnalytics.Application.Logs;

public abstract class CombatLogParser
{
    private static readonly string[] TimestampFormats = { "M/d/yyyy H:mm:ss.ffff", "M/d H:mm:ss.fff", "M/d H:mm:ss" };

    /// <summary>
    /// Parses a single combat log text line into a <see cref="ParsedCombatLogEvent"/>.
    /// </summary>
    /// <param name="line">A combat log line that begins with a timestamp, followed by two spaces and a comma-separated sequence of fields that align with <c>CombatLogFieldMappings</c>.</param>
    /// <returns>A <see cref="ParsedCombatLogEvent"/> populated from the line, or <c>null</c> if the input is empty, malformed, or its timestamp cannot be parsed.</returns>
    public static ParsedCombatLogEvent? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        var parts = line.Split(["  "], 2, StringSplitOptions.None);
        if (parts.Length != 2) return null;

        if (!TryParseTimestamp(parts[0], out var ts)) return null;

        var fields = parts[1].Split(',');
        if (fields.Length == 0) return null;
        var evt = fields[CombatLogFieldMappings.Common.Event].Trim();

        switch (evt)
        {
            case CombatLogEventTypes.ZoneChange:
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
            case CombatLogEventTypes.ArenaMatchStart:
            {
                var arenaMatchId = SafeField(fields, CombatLogFieldMappings.ArenaMatchStart.ArenaMatchId);
                var zoneId = ParseInt(SafeField(fields, CombatLogFieldMappings.ArenaMatchStart.ZoneId));
                return new ParsedCombatLogEvent
                {
                    Timestamp = ts,
                    EventType = evt,
                    ArenaMatchId = TrimQuotes(arenaMatchId),
                    ZoneId = zoneId
                };
            }
        }

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

    /// <summary>
/// Determines whether the provided zone identifier corresponds to an arena zone.
/// </summary>
/// <param name="zoneId">The numeric identifier of the zone to check.</param>
/// <returns>`true` if the zone is an arena zone, `false` otherwise.</returns>
public static bool IsArenaZone(int zoneId) => ArenaZoneIds.IsArena(zoneId);

    /// <summary>
    /// Attempts to parse a timestamp string from a combat log into a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="input">The timestamp string to parse.</param>
    /// <param name="timestamp">When this method returns, contains the parsed timestamp converted to UTC if parsing succeeded; otherwise <see cref="DateTime.MinValue"/>.</param>
    /// <returns>`true` if the string was parsed successfully and <paramref name="timestamp"/> contains the parsed UTC time, `false` otherwise.</returns>
    private static bool TryParseTimestamp(string input, out DateTime timestamp)
    {
        return DateTime.TryParseExact(input, TimestampFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out timestamp) || DateTime.TryParse(input,
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out timestamp);
    }

    /// <summary>
/// Gets the element at the specified index from the string array, or returns an empty string if the index is out of range.
/// </summary>
/// <param name="arr">The array of string fields.</param>
/// <param name="idx">The zero-based index of the element to retrieve.</param>
/// <returns>The element at the specified index, or an empty string when the index is outside the array bounds.</returns>
private static string SafeField(string[] arr, int idx) => idx < arr.Length ? arr[idx] : string.Empty;
    /// <summary>
/// Parses an integer from a string that may be surrounded by double quotes and returns null if parsing fails.
/// </summary>
/// <param name="s">The input string which may contain surrounding double quotes and whitespace.</param>
/// <returns>The parsed `int` if the input can be converted, `null` otherwise.</returns>
private static int? ParseInt(string s) => int.TryParse(TrimQuotes(s), out var v) ? v : null;
    /// <summary>
/// Trims leading and trailing spaces and double-quote characters from the given string.
/// </summary>
/// <param name="s">The input string to trim.</param>
/// <returns>The input string with surrounding spaces and double quotes removed.</returns>
private static string TrimQuotes(string s) => s.Trim(' ', '"');
}