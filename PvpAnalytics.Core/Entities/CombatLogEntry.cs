namespace PvpAnalytics.Core.Entities;

public class CombatLogEntry
{
    public long Id { get; set; }

    public long MatchId { get; set; }
    public Match Match { get; set; }

    public DateTime Timestamp { get; set; }

    public long SourcePlayerId { get; set; }
    public Player SourcePlayer { get; set; }

    public long? TargetPlayerId { get; set; }
    public Player TargetPlayer { get; set; }

    public string Ability { get; set; }
    public int DamageDone { get; set; }
    public int HealingDone { get; set; }
    public string CrowdControl { get; set; }
}
