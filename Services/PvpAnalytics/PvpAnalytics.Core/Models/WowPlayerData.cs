namespace PvpAnalytics.Core.Models;

/// <summary>
/// Represents player data retrieved from Blizzard WoW API.
/// </summary>
public class WowPlayerData
{
    public string? Class { get; set; }
    public int? Level { get; set; }
    public string? Faction { get; set; }
    public string? Race { get; set; }
    public string? Realm { get; set; }
    public string? Name { get; set; }
}

