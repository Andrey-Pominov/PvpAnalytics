using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.DTOs;

public class MatchDetailDto
{
    public MatchBasicInfo BasicInfo { get; set; } = new();
    public List<TeamInfo> Teams { get; set; } = new();
    public List<TimelineEvent> TimelineEvents { get; set; } = new();
}

public class MatchBasicInfo
{
    public long Id { get; set; }
    public string UniqueHash { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string MapName { get; set; } = string.Empty;
    public ArenaZone ArenaZone { get; set; }
    public GameMode GameMode { get; set; }
    public long Duration { get; set; }
    public bool IsRanked { get; set; }
}

public class ParticipantInfo
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Team { get; set; } = string.Empty;
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public bool IsWinner { get; set; }
    public long TotalDamage { get; set; }
    public long TotalHealing { get; set; }
    public int TotalCC { get; set; }
}

public class TeamInfo
{
    public string TeamName { get; set; } = string.Empty;
    public List<ParticipantInfo> Participants { get; set; } = new();
    public long TotalDamage { get; set; }
    public long TotalHealing { get; set; }
    public bool IsWinner { get; set; }
}

public class TimelineEvent
{
    public long Timestamp { get; set; } // Relative to match start in seconds
    public string EventType { get; set; } = string.Empty; // "damage", "healing", "cc", "cooldown", "kill"
    public long? SourcePlayerId { get; set; }
    public string? SourcePlayerName { get; set; }
    public long? TargetPlayerId { get; set; }
    public string? TargetPlayerName { get; set; }
    public string Ability { get; set; } = string.Empty;
    public long? DamageDone { get; set; }
    public long? HealingDone { get; set; }
    public string? CrowdControl { get; set; }
    public bool IsImportant { get; set; } // Cooldown/defensive/CC flag
    public bool IsCooldown { get; set; }
    public bool IsCC { get; set; }
}

