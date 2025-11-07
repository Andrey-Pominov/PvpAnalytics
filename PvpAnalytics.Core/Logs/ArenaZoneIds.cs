namespace PvpAnalytics.Core.Logs;

public static class ArenaZoneIds
{
    public static readonly IReadOnlyDictionary<int, string> Map =
        new Dictionary<int, string>
        {
            // 2759 seen in sample (Blood Ring / Mok'gol Training Grounds Arena variants)
            { 2759, "Blood Ring" },
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

    /// <summary>
/// Determines whether the specified zone ID corresponds to a known arena.
/// </summary>
/// <param name="zoneId">The zone identifier to check.</param>
/// <returns>`true` if the zone ID maps to a known arena, `false` otherwise.</returns>
public static bool IsArena(int zoneId) => Map.ContainsKey(zoneId);

    /// <summary>
        /// Retrieves the arena name for the provided zone ID, or returns the specified fallback if not found.
        /// </summary>
        /// <param name="zoneId">Arena zone identifier.</param>
        /// <param name="fallback">Fallback name to return when the zone ID is not present; defaults to "Unknown Arena".</param>
        /// <returns>The arena name associated with <paramref name="zoneId"/>, or <paramref name="fallback"/> if absent.</returns>
        public static string GetNameOrDefault(int zoneId, string fallback = "Unknown Arena")
        => Map.GetValueOrDefault(zoneId, fallback);
}

