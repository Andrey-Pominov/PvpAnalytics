namespace PvpAnalytics.Core.Entities;

public class MatchDiscussionPost
{
    public long Id { get; set; }
    public long ThreadId { get; set; }
    public MatchDiscussionThread Thread { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? ParentPostId { get; set; }
    public MatchDiscussionPost? ParentPost { get; set; }
    public int Upvotes { get; set; }
    public bool IsDeleted { get; set; }
}

