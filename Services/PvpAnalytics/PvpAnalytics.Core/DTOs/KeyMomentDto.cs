namespace PvpAnalytics.Core.DTOs;

public class KeyMomentDto
{
    public long MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public List<KeyMoment> Moments { get; set; } = new();
}

public class KeyMoment
{
    public long Timestamp { get; set; } // Relative to match start in seconds
    public string EventType { get; set; } = string.Empty; // "death", "cooldown", "cc_chain", "rating_change", "damage_spike"
    public string Description { get; set; } = string.Empty;
    public long? SourcePlayerId { get; set; }
    public string? SourcePlayerName { get; set; }
    public long? TargetPlayerId { get; set; }
    public string? TargetPlayerName { get; set; }
    public string? Ability { get; set; }
    public long? DamageDone { get; set; }
    public long? HealingDone { get; set; }
    public string? CrowdControl { get; set; }
    public double ImpactScore { get; set; } // 0-1, higher = more impactful
    public bool IsCritical { get; set; } // True for game-changing moments
}

public class PlayerKeyMomentsDto
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public List<KeyMomentDto> RecentMatches { get; set; } = new();
}

