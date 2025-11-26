namespace PvpAnalytics.Core.Entities;

public class CommunityRanking
{
    public long Id { get; set; }
    public string RankingType { get; set; } = string.Empty; // e.g., "MostWatched", "TopWinrate", "MostDiscussed"
    public string Period { get; set; } = string.Empty; // e.g., "Daily", "Weekly", "Monthly"
    public string? Scope { get; set; } // e.g., "Global", "Region", "Bracket"
    public long? PlayerId { get; set; }
    public Player? Player { get; set; }
    public long? TeamId { get; set; }
    public Team? Team { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public DateTime CalculatedAt { get; set; }
}

