namespace PvpAnalytics.Core.Entities;

public class Team
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Bracket { get; set; } // e.g., "2v2", "3v3", "RBG"
    public string? Region { get; set; }
    public Guid? CreatedByUserId { get; set; } // Link to AuthService user
    public bool IsPublic { get; set; } = true; // Visibility setting
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TeamMatch> TeamMatches { get; set; } = new List<TeamMatch>();
}

