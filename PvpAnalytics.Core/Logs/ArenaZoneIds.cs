using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.Logs;

public static class ArenaZoneIds
{
    // Map of zoneId -> (EnglishName, GameMode)
    public static readonly IReadOnlyDictionary<int, (string Name, GameMode Mode)> Map =
        new Dictionary<int, (string, GameMode)>
        {
            // 2759 seen in sample (Blood Ring / Mok'gol Training Grounds Arena variants)
            { 2759, ("Blood Ring", GameMode.TwoVsTwo) },
            // Common arena zone ids (examples; extend as needed)
            { 617, ("Dalaran Arena", GameMode.TwoVsTwo) },
            { 618, ("Ring of Valor", GameMode.TwoVsTwo) },
            { 572, ("Ruins of Lordaeron", GameMode.TwoVsTwo) },
            { 559, ("Nagrand Arena", GameMode.TwoVsTwo) },
            { 6178, ("Mugambala", GameMode.TwoVsTwo) },
            { 1505, ("The Tiger's Peak", GameMode.TwoVsTwo) },
            { 1504, ("Tol'viron Arena", GameMode.TwoVsTwo) },
            { 1825, ("Black Rook Hold Arena", GameMode.TwoVsTwo) },
            { 3963, ("Maldraxxus Coliseum", GameMode.TwoVsTwo) },
        };

    public static bool IsArena(int zoneId) => Map.ContainsKey(zoneId);

    public static GameMode GetGameModeOrDefault(int zoneId, GameMode fallback = GameMode.TwoVsTwo)
        => Map.TryGetValue(zoneId, out var v) ? v.Mode : fallback;

    public static string GetNameOrDefault(int zoneId, string fallback = "Unknown Arena")
        => Map.TryGetValue(zoneId, out var v) ? v.Name : fallback;
}


