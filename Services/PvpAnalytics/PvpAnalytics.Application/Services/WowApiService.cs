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
    private string? _accessToken;
    private DateTime? _tokenExpiry;

    public async Task<WowPlayerData?> GetPlayerDataAsync(string realm, string name, string region,
        CancellationToken ct = default)
    {
        try
        {
            if (!await EnsureValidAccessTokenAsync(region, ct))
                return null;

            var realmSlug = NormalizeRealmName(realm);
            var nameSlug = name.ToLowerInvariant();
            var baseUrl = GetBaseUrlForRegion(region);

            var profileData = await FetchProfileDataAsync(baseUrl, realmSlug, nameSlug, region, ct);

            var playerData = ParsePlayerData(profileData, name, realm);
            ParseClass(playerData, profileData);
            ParseRaceAndFaction(playerData, profileData);

            return playerData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching player data from WoW API for {Name} on {Realm}", name, realm);
            return null;
        }
    }

    private async Task<bool> EnsureValidAccessTokenAsync(string region, CancellationToken ct)
    {
        await EnsureAccessTokenAsync(region, ct);

        if (string.IsNullOrEmpty(_accessToken))
        {
            logger.LogWarning("Failed to obtain access token for WoW API");
            return false;
        }

        return true;
    }

    private static string GetBaseUrlForRegion(string region)
    {
        return region.Equals("eu", StringComparison.InvariantCultureIgnoreCase)
            ? "https://eu.api.blizzard.com"
            : "https://us.api.blizzard.com";
    }

    private async Task<JsonElement> FetchProfileDataAsync(
        string baseUrl, string realmSlug, string nameSlug, string region, CancellationToken ct)
    {
        var profileUrl = $"{baseUrl}/profile/wow/character/{realmSlug}/{nameSlug}?namespace=profile-{region}&locale=en_US";
        var response = await SendAuthenticatedRequestAsync(HttpMethod.Get, profileUrl, ct);


        if (response is not { IsSuccessStatusCode: true })
        {
            HandleProfileRequestError(response, realmSlug, nameSlug);
            return new JsonElement();
        }

        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    private async Task<HttpResponseMessage?> SendAuthenticatedRequestAsync(
        HttpMethod method, string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        return await httpClient.SendAsync(request, ct);
    }

    private void HandleProfileRequestError(HttpResponseMessage? response, string realm, string name)
    {
        if (response is { StatusCode: System.Net.HttpStatusCode.NotFound })
        {
            logger.LogDebug("Player {Name} on realm {Realm} not found in WoW API", name, realm);
        }
        else
        {
            logger.LogWarning("WoW API returned status {StatusCode} for {Name} on {Realm}",
                response?.StatusCode, name, realm);
        }
    }

    private static WowPlayerData ParsePlayerData(JsonElement profileData, string name, string realm)
    {
        return new WowPlayerData
        {
            Name = ExtractStringProperty(profileData, "name") ?? name,
            Realm = ExtractRealmSlug(profileData, realm),
            Level = ExtractIntProperty(profileData, "level")
        };
    }

    private static string? ExtractStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    private static int? ExtractIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : null;
    }

    private static string ExtractRealmSlug(JsonElement profileData, string defaultRealm)
    {
        if (!profileData.TryGetProperty("realm", out var realmProp))
            return defaultRealm;

        return realmProp.TryGetProperty("slug", out var realmSlugProp)
            ? realmSlugProp.GetString() ?? defaultRealm
            : defaultRealm;
    }

    private static void ParseClass(WowPlayerData playerData, JsonElement profileData)
    {
        if (!profileData.TryGetProperty("character_class", out var classProp))
            return;

        playerData.Class = ExtractStringProperty(classProp, "name");
    }

    private static void ParseRaceAndFaction(WowPlayerData playerData, JsonElement profileData)
    {
        if (!profileData.TryGetProperty("race", out var raceProp))
            return;

        playerData.Race = ExtractStringProperty(raceProp, "name");
        
        // Extract faction from API response (authoritative source)
        if (profileData.TryGetProperty("faction", out var factionProp))
        {
            playerData.Faction = ExtractStringProperty(factionProp, "name");
        }
        
        // Fallback to race-based inference only if faction is not available from API
        if (string.IsNullOrWhiteSpace(playerData.Faction) && playerData.Race != null)
        {
            playerData.Faction = DetermineFactionFromRace(playerData.Race);
        }
    }

    private static string? DetermineFactionFromRace(string race)
    {
        var allianceRaces = new[]
        {
            "Human", "Dwarf", "Night Elf", "Gnome", "Draenei", "Worgen", "Pandaren", "Void Elf",
            "Lightforged Draenei", "Dark Iron Dwarf", "Kul Tiran", "Mechagnome", "Dracthyr"
        };
        var hordeRaces = new[]
        {
            "Orc", "Undead", "Tauren", "Troll", "Blood Elf", "Goblin", "Pandaren", "Nightborne",
            "Highmountain Tauren", "Mag'har Orc", "Zandalari Troll", "Vulpera", "Dracthyr"
        };

        if (allianceRaces.Any(r => race.Contains(r, StringComparison.OrdinalIgnoreCase)))
            return "Alliance";

        if (hordeRaces.Any(r => race.Contains(r, StringComparison.OrdinalIgnoreCase)))
            return "Horde";

        return null;
    }

    private async Task EnsureAccessTokenAsync(string region, CancellationToken ct)
    {
        // Check if we have a valid token
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry.HasValue &&
            _tokenExpiry.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return; // Token is still valid
        }

        try
        {
            // Determine OAuth URL based on region
            var oauthUrl = region.Equals("eu"
                , StringComparison.InvariantCultureIgnoreCase)
                ? "https://eu.battle.net/oauth/token"
                : "https://us.battle.net/oauth/token";

            var requestBody = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]);

            var authValue =
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, oauthUrl);
            request.Content = requestBody;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to obtain OAuth token. Status: {StatusCode}", response.StatusCode);
                _accessToken = null;
                _tokenExpiry = null;
                return;
            }

            var tokenData = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            if (tokenData.TryGetProperty("access_token", out var tokenProp))
            {
                _accessToken = tokenProp.GetString();
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
                logger.LogDebug("Obtained new OAuth token for WoW API");
            }
            else
            {
                logger.LogError("OAuth token response missing access_token");
                _accessToken = null;
                _tokenExpiry = null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obtaining OAuth token for WoW API");
            _accessToken = null;
            _tokenExpiry = null;
        }
    }

    private static string NormalizeRealmName(string realm)
    {
        if (string.IsNullOrWhiteSpace(realm))
            return string.Empty;

        return realm.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("'", "");
    }
}