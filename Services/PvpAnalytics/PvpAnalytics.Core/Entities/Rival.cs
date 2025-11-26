namespace PvpAnalytics.Core.Entities;

public class Rival
{
    public long Id { get; set; }
    public Guid OwnerUserId { get; set; } // User from AuthService
    public long OpponentPlayerId { get; set; }
    public Player OpponentPlayer { get; set; } = null!;
    public string? Notes { get; set; }
    public int IntensityScore { get; set; } = 5; // 1-10 scale
    public DateTime CreatedAt { get; set; }
}

