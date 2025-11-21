namespace PvpAnalytics.Core.DTOs;

public class PlayerStatsDto
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public double AverageMatchDuration { get; set; }
    public string? FavoriteGameMode { get; set; }
    public string? FavoriteSpec { get; set; }
}

