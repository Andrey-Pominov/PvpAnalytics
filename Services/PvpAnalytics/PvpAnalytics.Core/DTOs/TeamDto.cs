namespace PvpAnalytics.Core.DTOs;

public class TeamDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Bracket { get; set; }
    public string? Region { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TeamMemberDto> Members { get; set; } = new();
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
}

public class TeamMemberDto
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public DateTime JoinedAt { get; set; }
    public string? Role { get; set; }
    public bool IsPrimary { get; set; }
}

public class CreateTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string? Bracket { get; set; }
    public string? Region { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<long> PlayerIds { get; set; } = new();
}

public class UpdateTeamDto
{
    public string? Name { get; set; }
    public string? Bracket { get; set; }
    public string? Region { get; set; }
    public bool? IsPublic { get; set; }
}

