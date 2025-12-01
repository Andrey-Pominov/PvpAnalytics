using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Core.Logs;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Extracts and analyzes player information from combat log data.
/// </summary>
public static class PlayerInfoExtractor
{
    /// <summary>
    /// Parses a player name string to extract Name and Realm.
    /// Format: "Name-Realm-Region" or "Name-Realm"
    /// Example: "Бигмэджик-СвежевательДуш-EU" → Name: "Бигмэджик", Realm: "СвежевательДуш"
    /// </summary>
    public static (string Name, string Realm) ParsePlayerName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return (string.Empty, string.Empty);

        var trimmed = fullName.Trim('"', ' ');
        if (string.IsNullOrEmpty(trimmed))
            return (string.Empty, string.Empty);

        var regionSuffixes = new[] { "-EU", "-US", "-KR", "-TW", "-CN" };
        var suffix = regionSuffixes
            .FirstOrDefault(s => trimmed.EndsWith(s, StringComparison.OrdinalIgnoreCase));

        if (suffix is not null)
        {
            trimmed = trimmed[..^suffix.Length];
        }

        var firstDash = trimmed.IndexOf('-');
        switch (firstDash)
        {
            case < 0:
                return (trimmed, string.Empty);
            case 0:
                return (string.Empty, trimmed[1..]);
        }

        var name = trimmed[..firstDash];
        var realm = trimmed[(firstDash + 1)..];

        return (name, realm);
    }

    /// <summary>
    /// Updates a player entity with information extracted from tracked spells.
    /// Only updates fields that are currently empty.
    /// </summary>
    public static void UpdatePlayerFromSpells(Player player, HashSet<string> spells)
    {
        if (spells.Count == 0)
            return;

        // Determine class if not set
        if (string.IsNullOrWhiteSpace(player.Class))
        {
            var detectedClass = PlayerAttributeMappings.DetermineClass(spells);
            if (!string.IsNullOrWhiteSpace(detectedClass))
            {
                player.Class = detectedClass;
            }
        }

        // Determine spec if not set (can be updated even if class is already set)
        if (string.IsNullOrWhiteSpace(player.Spec))
        {
            var detectedSpec = PlayerAttributeMappings.DetermineSpec(spells);
            if (!string.IsNullOrWhiteSpace(detectedSpec))
            {
                player.Spec = detectedSpec;
            }
        }

        // Determine faction/race if not set
        if (!string.IsNullOrWhiteSpace(player.Faction)) return;

        var detectedFaction = PlayerAttributeMappings.DetermineFaction(spells);
        if (!string.IsNullOrWhiteSpace(detectedFaction))
        {
            player.Faction = detectedFaction;
        }
    }

    /// <summary>
    /// Determines the spec for a match based on tracked spells.
    /// This is used to get match-specific spec (player may switch specs between matches).
    /// </summary>
    public static string DetermineSpecForMatch(HashSet<string> spells)
    {
        if (spells == null || spells.Count == 0)
            return string.Empty;

        return PlayerAttributeMappings.DetermineSpec(spells) ?? string.Empty;
    }
}