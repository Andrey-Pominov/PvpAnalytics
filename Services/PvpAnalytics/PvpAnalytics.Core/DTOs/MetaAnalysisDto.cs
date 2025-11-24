using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.DTOs;

public class MetaAnalysisDto
{
    public int? RatingMin { get; set; }
    public int? RatingMax { get; set; }
    public GameMode? GameMode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<CompositionMeta> Compositions { get; set; } = new();
    public MetaTrends Trends { get; set; } = new();
}

public class CompositionMeta
{
    public string Composition { get; set; } = string.Empty; // e.g., "Rogue-Mage-Priest"
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public double WinRate { get; set; }
    public double Popularity { get; set; } // Percentage of total matches
    public double AverageRating { get; set; }
    public int Rank { get; set; } // Rank by popularity
}

public class MetaTrends
{
    public string Composition { get; set; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; set; } = new();
}

public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public int Matches { get; set; }
    public double Popularity { get; set; }
    public double WinRate { get; set; }
}

