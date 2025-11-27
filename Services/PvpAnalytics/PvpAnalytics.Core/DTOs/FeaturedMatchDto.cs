namespace PvpAnalytics.Core.DTOs;

public class FeaturedMatchDto
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public DateTime FeaturedAt { get; set; }
    public string? Reason { get; set; }
    public int Upvotes { get; set; }
    public int CommentsCount { get; set; }
    public MatchDetailDto? Match { get; set; }
}

