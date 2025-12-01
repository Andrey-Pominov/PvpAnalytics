namespace PvpAnalytics.Core.Entities;

public class Player
{
    public Player()
    {
        Name = string.Empty;
        Realm = string.Empty;
        Class = string.Empty;
        Faction = string.Empty;
        Spec = string.Empty;
        MatchResults = [];
        SourceCombatLogs = [];
        TargetCombatLogs = [];
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public string Realm { get; set; }
    public string Class { get; set; }
    public string Faction { get; set; }
    public string Spec { get; set; }

    public ICollection<MatchResult> MatchResults { get; set; } = [];
    public ICollection<CombatLogEntry> SourceCombatLogs { get; set; } = [];
    public ICollection<CombatLogEntry> TargetCombatLogs { get; set; } = [];
}