namespace PvpAnalytics.Core.DTOs;

public class CommunityRankingDto
{
    public string RankingType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public List<RankingEntryDto> Entries { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class RankingEntryDto
{
    public int Rank { get; set; }
    public long? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public long? TeamId { get; set; }
    public string? TeamName { get; set; }
    public double Score { get; set; }
}

