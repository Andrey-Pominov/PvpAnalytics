namespace PvpAnalytics.Core.Entities;

public class MatchDiscussionThread
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Title { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPinned { get; set; }

    public ICollection<MatchDiscussionPost> Posts { get; set; } = new List<MatchDiscussionPost>();
}

