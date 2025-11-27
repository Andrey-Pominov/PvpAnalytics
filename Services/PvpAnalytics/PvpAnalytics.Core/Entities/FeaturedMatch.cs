namespace PvpAnalytics.Core.Entities;

public class FeaturedMatch
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public DateTime FeaturedAt { get; set; }
    public string? Reason { get; set; } // e.g., "High Rating", "Intense Match", "Upset"
    public Guid? CuratorUserId { get; set; } // User who featured it
    public int Upvotes { get; set; }
    public int CommentsCount { get; set; }
}

