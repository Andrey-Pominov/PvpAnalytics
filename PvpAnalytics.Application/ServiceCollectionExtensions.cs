using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Configuration;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Application.Logs;

namespace PvpAnalytics.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICrudService<Player>, PlayerService>();
        services.AddScoped<ICrudService<Match>, MatchService>();
        services.AddScoped<ICrudService<MatchResult>, MatchResultService>();
        services.AddScoped<ICrudService<CombatLogEntry>, CombatLogEntryService>();
        services.AddScoped<ICombatLogIngestionService, CombatLogIngestionService>();
        
        // Configure WoW API with validation
        services.AddOptions<WowApiOptions>()
            .Bind(configuration.GetSection(WowApiOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ClientId))
                {
                    throw new ValidationException("WowApi:ClientId is required. Please configure it in appsettings.json or environment variables.");
                }
                if (string.IsNullOrWhiteSpace(options.ClientSecret))
                {
                    throw new ValidationException("WowApi:ClientSecret is required. Please configure it in appsettings.json or environment variables.");
                }
                return true;
            }, "WowApi credentials must be configured.")
            .ValidateOnStart(); // Fail fast on startup
        
        services.AddHttpClient<WowApiService>();
        services.AddScoped<IWowApiService, WowApiService>();
        
        return services;
    }
}


