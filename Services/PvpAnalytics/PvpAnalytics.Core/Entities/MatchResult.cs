namespace PvpAnalytics.Core.Entities;

public class MatchResult
{
    public MatchResult()
    {
        Match = null!;
        Player = null!;
        Team = string.Empty;
    }

    public long Id { get; set; }

    public long MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public long PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public string Team { get; set; } = string.Empty;
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public bool IsWinner { get; set; }
    public string? Spec { get; set; }
}