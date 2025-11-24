using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.DTOs;

public class RatingProgressionDto
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public GameMode? GameMode { get; set; }
    public string? Spec { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<RatingDataPoint> DataPoints { get; set; } = new();
    public RatingSummary Summary { get; set; } = new();
}

public class RatingDataPoint
{
    public DateTime MatchDate { get; set; }
    public long MatchId { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public int RatingChange { get; set; }
    public bool IsWinner { get; set; }
    public string? Spec { get; set; }
    public GameMode GameMode { get; set; }
}

public class RatingSummary
{
    public int CurrentRating { get; set; }
    public int PeakRating { get; set; }
    public int LowestRating { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingGain { get; set; }
    public int TotalRatingLoss { get; set; }
    public int NetRatingChange { get; set; }
}

