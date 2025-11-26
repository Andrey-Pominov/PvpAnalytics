namespace PvpAnalytics.Core.DTOs;

public class RivalDto
{
    public long Id { get; set; }
    public long OpponentPlayerId { get; set; }
    public string OpponentPlayerName { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Notes { get; set; }
    public int IntensityScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
}

public class CreateRivalDto
{
    public long PlayerId { get; set; }
    public string? Notes { get; set; }
    public int IntensityScore { get; set; } = 5;
}

public class UpdateRivalDto
{
    public string? Notes { get; set; }
    public int? IntensityScore { get; set; }
}

