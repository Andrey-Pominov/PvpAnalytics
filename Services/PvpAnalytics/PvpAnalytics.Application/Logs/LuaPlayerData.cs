namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Represents player data parsed from the new Lua table format.
/// </summary>
public class LuaPlayerData
{
    public string PlayerGuid { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Realm { get; set; }
    public string? ClassId { get; set; }
    public string? Class { get; set; }
    public int? SpecId { get; set; }
    public string? Faction { get; set; }
    public double? KdRatio { get; set; }
    public int? Losses { get; set; }
    public int? Wins { get; set; }
    public int? MatchesPlayed { get; set; }
    public long? TotalDamage { get; set; }
    public long? TotalHealing { get; set; }
    public double? InterruptsPerMatch { get; set; }
}

