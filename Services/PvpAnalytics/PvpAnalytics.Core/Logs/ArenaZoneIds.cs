using PvpAnalytics.Core.Enum;

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

    /// <summary>
    /// Converts a zone ID to an ArenaZone enum value.
    /// </summary>
    /// <param name="zoneId">The zone identifier.</param>
    /// <returns>The ArenaZone enum value, or ArenaZone.Unknown if not found.</returns>
    public static ArenaZone GetArenaZone(int zoneId)
    {
        return zoneId switch
        {
            2759 => ArenaZone.BloodRing,
            617 => ArenaZone.DalaranArena,
            618 => ArenaZone.RingOfValor,
            572 => ArenaZone.RuinsOfLordaeron,
            559 => ArenaZone.NagrandArena,
            6178 => ArenaZone.Mugambala,
            1505 => ArenaZone.TheTigersPeak,
            1504 => ArenaZone.TolvironArena,
            1825 => ArenaZone.BlackRookHoldArena,
            3963 => ArenaZone.MaldraxxusColiseum,
            _ => ArenaZone.Unknown
        };
    }
}

