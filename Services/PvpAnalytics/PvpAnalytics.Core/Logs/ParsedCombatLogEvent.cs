namespace PvpAnalytics.Core.Logs;

public class ParsedCombatLogEvent
{
    public DateTime Timestamp { get; init; }
    public required string EventType { get; init; }

    public string? SourceGuid { get; init; }
    public string? SourceName { get; init; }
    public string? TargetGuid { get; init; }
    public string? TargetName { get; init; }

    public int? SpellId { get; init; }
    public string? SpellName { get; init; }

    public int? Damage { get; init; }
    public int? Healing { get; init; }
    public int? Absorbed { get; init; }

    public int? ZoneId { get; init; }
    public string? ZoneName { get; init; }
    public string? ArenaMatchId { get; init; }
}


