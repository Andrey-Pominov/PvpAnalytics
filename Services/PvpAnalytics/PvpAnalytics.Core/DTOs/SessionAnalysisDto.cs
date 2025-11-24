namespace PvpAnalytics.Core.DTOs;

public class SessionAnalysisDto
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int ThresholdMinutes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<SessionData> Sessions { get; set; } = new();
    public SessionSummary Summary { get; set; } = new();
    public OptimalPlayTimes OptimalTimes { get; set; } = new();
    public FatigueAnalysis Fatigue { get; set; } = new();
}

public class SessionData
{
    public long SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int MatchCount { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public int RatingStart { get; set; }
    public int RatingEnd { get; set; }
    public int RatingChange { get; set; }
    public double AverageMatchDuration { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public int HourOfDay { get; set; }
}

public class SessionSummary
{
    public int TotalSessions { get; set; }
    public double AverageSessionDuration { get; set; }
    public double AverageMatchesPerSession { get; set; }
    public double AverageWinRate { get; set; }
    public int TotalRatingGain { get; set; }
    public int TotalRatingLoss { get; set; }
    public int NetRatingChange { get; set; }
}

public class OptimalPlayTimes
{
    public List<TimeSlotPerformance> ByHour { get; set; } = new();
    public List<TimeSlotPerformance> ByDayOfWeek { get; set; } = new();
    public string BestHour { get; set; } = string.Empty;
    public string BestDay { get; set; } = string.Empty;
}

public class TimeSlotPerformance
{
    public string TimeSlot { get; set; } = string.Empty; // "0-1", "Monday", etc.
    public int Sessions { get; set; }
    public double WinRate { get; set; }
    public double AverageRatingChange { get; set; }
}

public class FatigueAnalysis
{
    public List<SessionPerformanceByOrder> PerformanceByOrder { get; set; } = new();
    public bool ShowsFatigue { get; set; }
    public string FatiguePattern { get; set; } = string.Empty; // "Declining", "Stable", "Improving"
    public int OptimalSessionLength { get; set; } // In minutes
}

public class SessionPerformanceByOrder
{
    public int SessionOrder { get; set; } // 1st session of day, 2nd, etc.
    public int SessionCount { get; set; }
    public double AverageWinRate { get; set; }
    public double AverageRatingChange { get; set; }
}

