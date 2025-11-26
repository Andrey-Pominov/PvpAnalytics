namespace PvpAnalytics.Core.Entities;

public class UserBadge
{
    public long Id { get; set; }
    public Guid UserId { get; set; } // User from AuthService
    public string BadgeType { get; set; } = string.Empty; // e.g., "FirstWin", "Rating1000", "TeamPlayer"
    public string BadgeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EarnedAt { get; set; }
}

