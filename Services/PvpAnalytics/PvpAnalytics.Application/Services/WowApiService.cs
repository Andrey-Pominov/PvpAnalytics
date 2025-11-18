using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PvpAnalytics.Core.Configuration;
using PvpAnalytics.Core.Models;

namespace PvpAnalytics.Application.Services;

/// <summary>
/// Implementation of Blizzard WoW API service using OAuth2 authentication.
/// </summary>
public class WowApiService(
    HttpClient httpClient,
    IOptions<WowApiOptions> options,
    ILogger<WowApiService> logger) : IWowApiService
{
    private readonly WowApiOptions _options = options.Value;
    private readonly ILogger<WowApiService> _logger = logger;
    private string? _accessToken;
    private DateTime? _tokenExpiry;

    public async Task<WowPlayerData?> GetPlayerDataAsync(string realm, string name, string region, CancellationToken ct = default)
    {
        try
        {
            // Ensure we have a valid access token
            await EnsureAccessTokenAsync(region, ct);

            if (string.IsNullOrEmpty(_accessToken))
            {
                _logger.LogWarning("Failed to obtain access token for WoW API");
                return null;
            }

            // Normalize realm name (convert to slug format)
            var realmSlug = NormalizeRealmName(realm);
            var nameSlug = name.ToLowerInvariant();

            // Determine base URL based on region
            var baseUrl = region.ToLowerInvariant() == "eu" 
                ? "https://eu.api.blizzard.com" 
                : "https://us.api.blizzard.com";

            // Fetch character profile
            var profileUrl = $"{baseUrl}/profile/wow/character/{realmSlug}/{nameSlug}?namespace=profile-{region}&locale=en_US";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, profileUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Player {Name} on realm {Realm} not found in WoW API", name, realm);
                    return null;
                }
                
                _logger.LogWarning("WoW API returned status {StatusCode} for {Name} on {Realm}", 
                    response.StatusCode, name, realm);
                return null;
            }

            var profileData = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            
            // Fetch character media/summary for additional info
            var summaryUrl = $"{baseUrl}/profile/wow/character/{realmSlug}/{nameSlug}/character-media?namespace=profile-{region}&locale=en_US";
            using var summaryRequest = new HttpRequestMessage(HttpMethod.Get, summaryUrl);
            summaryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            
            var summaryResponse = await httpClient.SendAsync(summaryRequest, ct);
            JsonElement? summaryData = null;
            if (summaryResponse.IsSuccessStatusCode)
            {
                summaryData = await summaryResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
            }

            // Parse the response
            var playerData = new WowPlayerData
            {
                Name = profileData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : name,
                Realm = profileData.TryGetProperty("realm", out var realmProp) 
                    ? realmProp.TryGetProperty("slug", out var realmSlugProp) ? realmSlugProp.GetString() : realm 
                    : realm,
                Level = profileData.TryGetProperty("level", out var levelProp) ? levelProp.GetInt32() : null,
            };

            // Parse class
            if (profileData.TryGetProperty("character_class", out var classProp))
            {
                if (classProp.TryGetProperty("name", out var classNameProp))
                {
                    playerData.Class = classNameProp.GetString();
                }
            }

            // Parse race (can infer faction from race)
            if (profileData.TryGetProperty("race", out var raceProp))
            {
                if (raceProp.TryGetProperty("name", out var raceNameProp))
                {
                    playerData.Race = raceNameProp.GetString();
                    // Infer faction from race (simplified - would need full race list)
                    if (playerData.Race != null)
                    {
                        var allianceRaces = new[] { "Human", "Dwarf", "Night Elf", "Gnome", "Draenei", "Worgen", "Pandaren", "Void Elf", "Lightforged Draenei", "Dark Iron Dwarf", "Kul Tiran", "Mechagnome", "Dracthyr" };
                        var hordeRaces = new[] { "Orc", "Undead", "Tauren", "Troll", "Blood Elf", "Goblin", "Pandaren", "Nightborne", "Highmountain Tauren", "Mag'har Orc", "Zandalari Troll", "Vulpera", "Dracthyr" };
                        
                        if (allianceRaces.Any(r => playerData.Race.Contains(r, StringComparison.OrdinalIgnoreCase)))
                            playerData.Faction = "Alliance";
                        else if (hordeRaces.Any(r => playerData.Race.Contains(r, StringComparison.OrdinalIgnoreCase)))
                            playerData.Faction = "Horde";
                    }
                }
            }

            return playerData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching player data from WoW API for {Name} on {Realm}", name, realm);
            return null;
        }
    }

    private async Task EnsureAccessTokenAsync(string region, CancellationToken ct)
    {
        // Check if we have a valid token
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry.HasValue && _tokenExpiry.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return; // Token is still valid
        }

        try
        {
            // Determine OAuth URL based on region
            var oauthUrl = region.ToLowerInvariant() == "eu"
                ? "https://eu.battle.net/oauth/token"
                : "https://us.battle.net/oauth/token";

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            
            using var request = new HttpRequestMessage(HttpMethod.Post, oauthUrl)
            {
                Content = requestBody
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            var response = await httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to obtain OAuth token. Status: {StatusCode}", response.StatusCode);
                _accessToken = null;
                _tokenExpiry = null;
                return;
            }

            var tokenData = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            
            if (tokenData.TryGetProperty("access_token", out var tokenProp))
            {
                _accessToken = tokenProp.GetString();
                // Tokens typically expire in 1 hour, but we'll refresh 5 minutes early
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
                _logger.LogDebug("Obtained new OAuth token for WoW API");
            }
            else
            {
                _logger.LogError("OAuth token response missing access_token");
                _accessToken = null;
                _tokenExpiry = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining OAuth token for WoW API");
            _accessToken = null;
            _tokenExpiry = null;
        }
    }

    private static string NormalizeRealmName(string realm)
    {
        if (string.IsNullOrWhiteSpace(realm))
            return string.Empty;

        // Convert realm name to slug format (lowercase, replace spaces with hyphens)
        return realm.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("'", "");
    }
}

