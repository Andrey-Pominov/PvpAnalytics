namespace PvpAnalytics.Core.Logs;

public static class ArenaZoneIds
{
    // Map of zoneId -> EnglishName (GameMode computed from participant count)
    public static readonly IReadOnlyDictionary<int, string> Map =
        new Dictionary<int, string>
        {
            // 2759 seen in sample (Blood Ring / Mok'gol Training Grounds Arena variants)
            { 2759, "Blood Ring" },
            // Common arena zone ids (examples; extend as needed)
            { 617, "Dalaran Arena" },
            { 618, "Ring of Valor" },
            { 572, "Ruins of Lordaeron" },
            { 559, "Nagrand Arena" },
            { 6178, "Mugambala" },
            { 1505, "The Tiger's Peak" },
            { 1504, "Tol'viron Arena" },
            { 1825, "Black Rook Hold Arena" },
            { 3963, "Maldraxxus Coliseum" },
        };

    public static bool IsArena(int zoneId) => Map.ContainsKey(zoneId);

    public static string GetNameOrDefault(int zoneId, string fallback = "Unknown Arena")
        => Map.TryGetValue(zoneId, out var name) ? name : fallback;
}


