namespace PvpAnalytics.Core.Entities;

public class TeamMember
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public Team Team { get; set; } = null!;
    
    public long PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    
    public DateTime JoinedAt { get; set; }
    public string? Role { get; set; } // e.g., "Leader", "Member"
    public bool IsPrimary { get; set; } // Primary team for this player
}

