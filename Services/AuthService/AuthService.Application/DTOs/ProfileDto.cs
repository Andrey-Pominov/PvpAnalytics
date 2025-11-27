namespace AuthService.Application.DTOs;

public class ProfileDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsProfilePublic { get; set; }
    public bool ShowStatsToFriendsOnly { get; set; }
}

public class UpdateProfileDto
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? IsProfilePublic { get; set; }
    public bool? ShowStatsToFriendsOnly { get; set; }
}

