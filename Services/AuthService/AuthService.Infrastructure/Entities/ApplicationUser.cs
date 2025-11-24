using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
}


