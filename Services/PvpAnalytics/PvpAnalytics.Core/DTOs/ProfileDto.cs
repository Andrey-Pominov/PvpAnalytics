namespace PvpAnalytics.Core.DTOs;

public class ProfileDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsProfilePublic { get; set; }
    public bool ShowStatsToFriendsOnly { get; set; }
    public List<BadgeDto> Badges { get; set; } = new();
}

public class UpdateProfileDto
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? IsProfilePublic { get; set; }
    public bool? ShowStatsToFriendsOnly { get; set; }
}

public class BadgeDto
{
    public long Id { get; set; }
    public string BadgeType { get; set; } = string.Empty;
    public string BadgeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EarnedAt { get; set; }
}

