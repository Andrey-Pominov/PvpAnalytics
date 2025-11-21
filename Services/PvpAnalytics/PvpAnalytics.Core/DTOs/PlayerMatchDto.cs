namespace PvpAnalytics.Core.DTOs;

public class PlayerMatchDto
{
    public long MatchId { get; set; }
    public DateTime CreatedOn { get; set; }
    public string MapName { get; set; } = string.Empty;
    public string GameMode { get; set; } = string.Empty;
    public long Duration { get; set; }
    public bool IsWinner { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public int RatingChange { get; set; }
    public string? Spec { get; set; }
    public string Team { get; set; } = string.Empty;
}

