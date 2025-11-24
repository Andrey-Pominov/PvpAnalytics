using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.DTOs;

public class MatchupAnalyticsDto
{
    public string Class1 { get; set; } = string.Empty;
    public string? Spec1 { get; set; }
    public string Class2 { get; set; } = string.Empty;
    public string? Spec2 { get; set; }
    public GameMode? GameMode { get; set; }
    public int? RatingMin { get; set; }
    public int? RatingMax { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TotalMatches { get; set; }
    public int WinsForClass1 { get; set; }
    public int WinsForClass2 { get; set; }
    public double WinRateForClass1 { get; set; }
    public double WinRateForClass2 { get; set; }
    public double AverageMatchDuration { get; set; }
    public MatchupDamageHealingStats Stats { get; set; } = new();
    public List<string> CommonStrategies { get; set; } = new();
}

public class MatchupDamageHealingStats
{
    public double AverageDamageClass1 { get; set; }
    public double AverageDamageClass2 { get; set; }
    public double AverageHealingClass1 { get; set; }
    public double AverageHealingClass2 { get; set; }
    public double AverageCCClass1 { get; set; }
    public double AverageCCClass2 { get; set; }
}

public class PlayerMatchupSummaryDto
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public List<MatchupWinRate> Weaknesses { get; set; } = new(); // Worst matchups
    public List<MatchupWinRate> Strengths { get; set; } = new(); // Best matchups
}

public class MatchupWinRate
{
    public string OpponentClass { get; set; } = string.Empty;
    public string? OpponentSpec { get; set; }
    public int Matches { get; set; }
    public int Wins { get; set; }
    public double WinRate { get; set; }
}

