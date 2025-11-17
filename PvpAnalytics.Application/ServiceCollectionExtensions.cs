using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        
        // Configure WoW API
        services.Configure<WowApiOptions>(configuration.GetSection(WowApiOptions.SectionName));
        services.AddHttpClient<WowApiService>();
        services.AddScoped<IWowApiService, WowApiService>();
        
        return services;
    }
}


