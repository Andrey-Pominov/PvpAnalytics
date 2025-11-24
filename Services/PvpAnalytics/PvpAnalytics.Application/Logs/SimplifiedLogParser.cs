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
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex InterruptPattern = new(
        @"(\d{2}:\d{2}:\d{2})\s*-\s*\|\cffff8800INTERRUPT:\|\r\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Alternative pattern for interrupts without exact color code matching
    private static readonly Regex InterruptPatternAlt = new(
        @"(\d{2}:\d{2}:\d{2})\s*-\s*.*?INTERRUPT.*?:\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        // Check for interrupt first (has special format with color codes)
        var interruptMatch = InterruptPattern.Match(logLine);
        if (!interruptMatch.Success)
        {
            interruptMatch = InterruptPatternAlt.Match(logLine);
        }
        if (interruptMatch.Success)
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

        // Determine event type from details
        string eventType;
        if (details.Contains("healed", StringComparison.OrdinalIgnoreCase))
            eventType = "HEAL";
        else if (details.Contains("used", StringComparison.OrdinalIgnoreCase))
            eventType = "DAMAGE";
        else
            return null;

        // Parse timestamp
        if (!TimeSpan.TryParse(timeStr, out var timeOfDay))
            return null;

        var timestamp = baseDate.Date.Add(timeOfDay);

        // Parse event type
        var parsedEventType = ParseEventType(eventType, details);
        if (parsedEventType == null)
            return null;

        // Extract source player and spell/ability
        var (sourceName, spellName, _) = ParseEventDetails(eventType, details, amountStr);

        // Parse amount
        int? damage = null;
        int? healing = null;

        if (!string.IsNullOrEmpty(amountStr) && int.TryParse(amountStr, out var parsedAmount))
        {
            if (eventType == "HEAL")
                healing = parsedAmount;
            else if (eventType == "DAMAGE")
                damage = parsedAmount;
        }

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

    private static ParsedCombatLogEvent? ParseInterruptEvent(Match match, DateTime baseDate)
    {
        var timeStr = match.Groups[1].Value;
        var details = match.Groups[2].Value;

        // Parse timestamp
        if (!TimeSpan.TryParse(timeStr, out var timeOfDay))
            return null;

        var timestamp = baseDate.Date.Add(timeOfDay);

        // Format: "PlayerName interrupted TargetName's SpellName"
        var interruptMatch = Regex.Match(details, @"^(.+?)\s+interrupted\s+(.+?)'s\s+(.+?)$", RegexOptions.IgnoreCase);
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

    private static string? ParseEventType(string eventType, string details)
    {
        return eventType switch
        {
            "HEAL" => CombatLogEventTypes.SpellHeal,
            "DAMAGE" => CombatLogEventTypes.SpellDamage,
            _ => null
        };
    }

    private static (string? sourceName, string? spellName, string? amount) ParseEventDetails(
        string eventType, string details, string? amountStr)
    {
        string? sourceName = null;
        string? spellName = null;

        if (eventType == "HEAL")
        {
            // Format: "PlayerName healed with SpellName for Amount"
            var healMatch = Regex.Match(details, @"^(.+?)\s+healed\s+with\s+(.+?)(?:\s+for\s+\d+)?$", RegexOptions.IgnoreCase);
            if (healMatch.Success)
            {
                sourceName = healMatch.Groups[1].Value.Trim();
                spellName = healMatch.Groups[2].Value.Trim();
            }
        }
        else if (eventType == "DAMAGE")
        {
            // Format: "PlayerName used SpellName for Amount on TargetName"
            var damageMatch = Regex.Match(details, @"^(.+?)\s+used\s+(.+?)(?:\s+for\s+\d+)?(?:\s+on\s+.+)?$", RegexOptions.IgnoreCase);
            if (damageMatch.Success)
            {
                sourceName = damageMatch.Groups[1].Value.Trim();
                spellName = damageMatch.Groups[2].Value.Trim();
            }
        }
        else if (eventType == "INTERRUPT")
        {
            // Format: "PlayerName interrupted TargetName's SpellName"
            var interruptMatch = Regex.Match(details, @"^(.+?)\s+interrupted\s+(.+?)'s\s+(.+?)$", RegexOptions.IgnoreCase);
            if (interruptMatch.Success)
            {
                sourceName = interruptMatch.Groups[1].Value.Trim();
                spellName = interruptMatch.Groups[3].Value.Trim();
            }
        }

        return (sourceName, spellName, amountStr);
    }
}

