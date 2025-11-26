namespace PvpAnalytics.Core.DTOs;

public class DiscussionThreadDto
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Title { get; set; }
    public int PostCount { get; set; }
    public List<DiscussionPostDto> Posts { get; set; } = new();
}

public class DiscussionPostDto
{
    public long Id { get; set; }
    public long ThreadId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? ParentPostId { get; set; }
    public int Upvotes { get; set; }
}

public class CreateThreadDto
{
    public long MatchId { get; set; }
    public string? Title { get; set; }
}

public class CreatePostDto
{
    public long ThreadId { get; set; }
    public string Body { get; set; } = string.Empty;
    public long? ParentPostId { get; set; }
}

