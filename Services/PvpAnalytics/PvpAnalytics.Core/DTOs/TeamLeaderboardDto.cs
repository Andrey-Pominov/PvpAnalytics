namespace PvpAnalytics.Core.DTOs;

public class TeamLeaderboardDto
{
    public string Bracket { get; set; } = string.Empty;
    public string? Region { get; set; }
    public List<TeamLeaderboardEntryDto> Entries { get; set; } = new();
    public int TotalTeams { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class TeamLeaderboardEntryDto
{
    public long TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public int Rank { get; set; }
    public DateTime LastMatchDate { get; set; }
    public List<string> MemberNames { get; set; } = new();
}

