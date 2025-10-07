using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.Entities;

public class Match
{
    public long Id { get; set; }
    public string UniqueHash  { get; set; }
    public DateTime CreatedOn { get; set; }
    public string MapName { get; set; }
    public GameMode GameMode { get; set; }
    public long Duration { get; set; }
    public bool IsRanked { get; set; }

    public ICollection<MatchResult> Results { get; set; }
    public ICollection<CombatLogEntry> CombatLogs { get; set; }
}