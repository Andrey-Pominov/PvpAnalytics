using System.ComponentModel.DataAnnotations;

namespace PvpAnalytics.Core.Configuration;

/// <summary>
/// Configuration options for Blizzard WoW API integration.
/// </summary>
public class WowApiOptions
{
    public const string SectionName = "WowApi";

    [Required(ErrorMessage = "WowApi:ClientId is required. Please configure it in appsettings.json or environment variables.")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "WowApi:ClientSecret is required. Please configure it in appsettings.json or environment variables.")]
    public string ClientSecret { get; set; } = string.Empty;
    
    public string BaseUrl { get; set; } = "https://us.api.blizzard.com";
    public string OAuthUrl { get; set; } = "https://us.battle.net/oauth/token";
}

