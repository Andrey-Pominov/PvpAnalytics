namespace PvpAnalytics.Core.Entities;

public class Player
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Realm { get; set; }
    public string Class { get; set; }
    public string Faction { get; set; }
    public string Spec { get; set; }

    public ICollection<MatchResult> MatchResults { get; set; }
    public ICollection<CombatLogEntry> SourceCombatLogs { get; set; }
    public ICollection<CombatLogEntry> TargetCombatLogs { get; set; }
}