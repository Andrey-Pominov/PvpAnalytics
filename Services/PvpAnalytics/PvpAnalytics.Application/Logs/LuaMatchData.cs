namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Represents a match parsed from Lua table format.
/// </summary>
public class LuaMatchData
{
    public List<string> Logs { get; set; } = new();
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Zone { get; set; }
    public string? Faction { get; set; }
    public string? Mode { get; set; }
    public Dictionary<string, object>? Statistics { get; set; }
    public List<LuaPlayerData> Players { get; set; } = new();
}

