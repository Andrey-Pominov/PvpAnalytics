using PvpAnalytics.Core.Models;

namespace PvpAnalytics.Application.Services;

/// <summary>
/// Service for interacting with Blizzard WoW API.
/// </summary>
public interface IWowApiService
{
    /// <summary>
    /// Gets player data from Blizzard API.
    /// </summary>
    /// <param name="realm">Realm name (slug format, e.g., "tichondrius")</param>
    /// <param name="name">Character name</param>
    /// <param name="region">Region code: "us" or "eu"</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Player data or null if not found or error occurred</returns>
    Task<WowPlayerData?> GetPlayerDataAsync(string realm, string name, string region, CancellationToken ct = default);
}

