using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Profile settings
    public bool IsProfilePublic { get; set; } = true;
    public bool ShowStatsToFriendsOnly { get; set; } = false;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}


