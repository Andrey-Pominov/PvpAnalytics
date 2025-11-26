namespace PvpAnalytics.Core.DTOs;

public class FavoritePlayerDto
{
    public long Id { get; set; }
    public long TargetPlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFavoritePlayerDto
{
    public long PlayerId { get; set; }
}

