using System.Globalization;
using System.Text.RegularExpressions;
using PvpAnalytics.Core.Logs;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Parses simplified combat log format: "HH:mm:ss - EVENT_TYPE: details"
/// </summary>
public static class SimplifiedLogParser
{
    // Pattern matches: "HH:mm:ss - EVENT_TYPE: details" or "HH:mm:ss - |cffff8800INTERRUPT:|r details"
    private static readonly Regex LogPattern = new(
        @"(\d{2}:\d{2}:\d{2})\s*-\s*(?:HEAL|DAMAGE):\s*(.+?)(?:\s+for\s+(\d+))?(?:\s+on\s+(.+?))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(150));

    private static readonly Regex InterruptPattern = new(
        @"(\d{2}:\d{2}:\d{2})\s*-\s*\|\cffff8800INTERRUPT:\|\r\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(150));

    // Alternative pattern for interrupts without exact color code matching
    private static readonly Regex InterruptPatternAlt = new(
        @"(\d{2}:\d{2}:\d{2})\s*-\s*.*?INTERRUPT.*?:\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(150));

    /// <summary>
    /// Parses a simplified log line into a ParsedCombatLogEvent.
    /// </summary>
    /// <param name="logLine">The log line in format "HH:mm:ss - EVENT_TYPE: details"</param>
    /// <param name="baseDate">Base date to combine with time from log line</param>
    /// <returns>Parsed event or null if parsing fails</returns>
    public static ParsedCombatLogEvent? ParseLine(string logLine, DateTime baseDate)
    {
        if (string.IsNullOrWhiteSpace(logLine))
            return null;

        var interruptMatch = TryMatchInterrupt(logLine);
        if (interruptMatch != null)
        {
            return ParseInterruptEvent(interruptMatch, baseDate);
        }

        var match = LogPattern.Match(logLine);
        if (!match.Success)
            return null;

        var timeStr = match.Groups[1].Value;
        var details = match.Groups[2].Value;
        var amountStr = match.Groups[3].Success ? match.Groups[3].Value : null;
        var targetStr = match.Groups[4].Success ? match.Groups[4].Value : null;

        var eventType = DetermineEventType(details);
        if (eventType == null)
            return null;

        if (!TryParseTimestamp(timeStr, baseDate, out var timestamp))
            return null;

        var parsedEventType = ParseEventType(eventType);
        if (parsedEventType == null)
            return null;

        var (sourceName, spellName) = ParseEventDetails(eventType, details);
        var (damage, healing) = ParseAmount(eventType, amountStr);

        return new ParsedCombatLogEvent
        {
            Timestamp = timestamp,
            EventType = parsedEventType,
            SourceName = sourceName,
            TargetName = targetStr,
            SpellName = spellName,
            Damage = damage,
            Healing = healing
        };
    }

    private static Match? TryMatchInterrupt(string logLine)
    {
        var match = InterruptPattern.Match(logLine);
        if (match.Success)
            return match;

        var altMatch = InterruptPatternAlt.Match(logLine);
        return altMatch.Success ? altMatch : null;
    }

    private static string? DetermineEventType(string details)
    {
        if (details.Contains("healed", StringComparison.OrdinalIgnoreCase))
            return "HEAL";

        if (details.Contains("used", StringComparison.OrdinalIgnoreCase))
            return "DAMAGE";

        return null;
    }

    private static bool TryParseTimestamp(string timeStr, DateTime baseDate, out DateTime timestamp)
    {
        timestamp = DateTime.MinValue;
        // Dash at position 0: name is missing, treat substring after dash as realm
        if (!TimeSpan.TryParse(timeStr, new CultureInfo("en-US"), out var timeOfDay))
            return false;

        timestamp = baseDate.Date.Add(timeOfDay);
        return true;
    }

    private static (int? damage, int? healing) ParseAmount(string eventType, string? amountStr)
    {
        if (string.IsNullOrEmpty(amountStr) || !int.TryParse(amountStr, out var parsedAmount))
            return (null, null);

        return eventType == "HEAL"
            ? (null, parsedAmount)
            : (parsedAmount, null);
    }

    private static ParsedCombatLogEvent? ParseInterruptEvent(Match match, DateTime baseDate)
    {
        var timeStr = match.Groups[1].Value;
        var details = match.Groups[2].Value;

        if (!TimeSpan.TryParse(timeStr, new CultureInfo("en-US"), out var timeOfDay))
            return null;

        var timestamp = baseDate.Date.Add(timeOfDay);

        // Format: "PlayerName interrupted TargetName's SpellName"
        var interruptMatch = Regex.Match(details, @"^(.+?)\s+interrupted\s+(.+?)'s\s+(.+?)$", RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(150));
        if (!interruptMatch.Success)
            return null;

        var sourceName = interruptMatch.Groups[1].Value.Trim();
        var targetName = interruptMatch.Groups[2].Value.Trim();
        var spellName = interruptMatch.Groups[3].Value.Trim();

        return new ParsedCombatLogEvent
        {
            Timestamp = timestamp,
            EventType = CombatLogEventTypes.SpellCastSuccess, // Use cast success as interrupt representation
            SourceName = sourceName,
            TargetName = targetName,
            SpellName = spellName
        };
    }

    private static string? ParseEventType(string eventType)
    {
        return eventType switch
        {
            "HEAL" => CombatLogEventTypes.SpellHeal,
            "DAMAGE" => CombatLogEventTypes.SpellDamage,
            _ => null
        };
    }

    private static (string? sourceName, string? spellName) ParseEventDetails(
        string eventType, string details)
    {
        string? sourceName = null;
        string? spellName = null;

        if (eventType == "HEAL")
        {
            // Format: "PlayerName healed with SpellName for Amount"
            var healMatch = Regex.Match(details, @"^(.+?)\s+healed\s+with\s+(.+?)(?:\s+for\s+\d+)?$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(150));
            if (healMatch.Success)
            {
                sourceName = healMatch.Groups[1].Value.Trim();
                spellName = healMatch.Groups[2].Value.Trim();
            }
        }
        else if (eventType == "DAMAGE")
        {
            // Format: "PlayerName used SpellName for Amount on TargetName"
            var damageMatch = Regex.Match(details, @"^(.+?)\s+used\s+(.+?)(?:\s+for\s+\d+)?(?:\s+on\s+.+)?$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(150));
            if (damageMatch.Success)
            {
                sourceName = damageMatch.Groups[1].Value.Trim();
                spellName = damageMatch.Groups[2].Value.Trim();
            }
        }
        else if (eventType == "INTERRUPT")
        {
            // Format: "PlayerName interrupted TargetName's SpellName"
            var interruptMatch = Regex.Match(details, @"^(.+?)\s+interrupted\s+(.+?)'s\s+(.+?)$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(150));
            if (interruptMatch.Success)
            {
                sourceName = interruptMatch.Groups[1].Value.Trim();
                spellName = interruptMatch.Groups[3].Value.Trim();
            }
        }

        return (sourceName, spellName);
    }
}