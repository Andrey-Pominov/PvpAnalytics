using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.DTOs;

public class MatchupQueryDto
{
    public string Class1 { get; set; } = null!;
    public string Class2 { get; set; } = null!;
    public string? Spec1 { get; set; }
    public string? Spec2 { get; set; }
    public GameMode? GameMode { get; set; }
    public int? RatingMin { get; set; }
    public int? RatingMax { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}