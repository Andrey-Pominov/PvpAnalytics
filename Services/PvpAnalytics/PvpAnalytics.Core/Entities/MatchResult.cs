namespace PvpAnalytics.Core.Entities;

public class MatchResult
{
    public long Id { get; set; }

    public long MatchId { get; set; }
    public Match Match { get; set; }

    public long PlayerId { get; set; }
    public Player Player { get; set; }

    public string Team { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public bool IsWinner { get; set; }
    public string? Spec { get; set; }
}