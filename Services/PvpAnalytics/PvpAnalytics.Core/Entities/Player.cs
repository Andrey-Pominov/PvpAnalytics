namespace PvpAnalytics.Core.Entities;

public class Player
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Faction { get; set; } = string.Empty;
    public string Spec { get; set; } = string.Empty;

    public ICollection<MatchResult> MatchResults { get; set; } = [];
    public ICollection<CombatLogEntry> SourceCombatLogs { get; set; } = [];
    public ICollection<CombatLogEntry> TargetCombatLogs { get; set; } = [];
}