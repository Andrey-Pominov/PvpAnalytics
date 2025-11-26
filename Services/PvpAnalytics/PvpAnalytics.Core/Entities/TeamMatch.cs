namespace PvpAnalytics.Core.Entities;

public class TeamMatch
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public Team Team { get; set; } = null!;
    
    public long MatchId { get; set; }
    public Match Match { get; set; } = null!;
    
    public bool IsWin { get; set; }
    public int? RatingChange { get; set; }
    public int? RatingBefore { get; set; }
    public int? RatingAfter { get; set; }
}

