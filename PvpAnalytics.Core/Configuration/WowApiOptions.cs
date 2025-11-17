namespace PvpAnalytics.Core.Configuration;

/// <summary>
/// Configuration options for Blizzard WoW API integration.
/// </summary>
public class WowApiOptions
{
    public const string SectionName = "WowApi";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://us.api.blizzard.com";
    public string OAuthUrl { get; set; } = "https://us.battle.net/oauth/token";
}

