namespace PvpAnalytics.Core.Entities;

public class FavoritePlayer
{
    public long Id { get; set; }
    public Guid OwnerUserId { get; set; } // User from AuthService
    public long TargetPlayerId { get; set; }
    public Player TargetPlayer { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

