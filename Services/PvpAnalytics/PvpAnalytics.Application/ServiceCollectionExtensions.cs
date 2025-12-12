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
        services.AddScoped<IMatchDetailService, MatchDetailService>();
        services.AddScoped<IOpponentScoutingService, OpponentScoutingService>();
        services.AddScoped<IMatchupAnalyticsService, MatchupAnalyticsService>();
        services.AddScoped<ITeamCompositionService, TeamCompositionService>();
        services.AddScoped<IRatingProgressionService, RatingProgressionService>();
        services.AddScoped<IKeyMomentService, KeyMomentService>();
        services.AddScoped<IMetaAnalysisService, MetaAnalysisService>();
        services.AddScoped<IPerformanceComparisonService, PerformanceComparisonService>();
        services.AddScoped<ISessionAnalysisService, SessionAnalysisService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ITeamSynergyService, TeamSynergyService>();
        services.AddScoped<ITeamLeaderboardService, TeamLeaderboardService>();
        services.AddScoped<IFavoritePlayerService, FavoritePlayerService>();
        services.AddScoped<IRivalService, RivalService>();
        services.AddScoped<IHighlightsService, HighlightsService>();
        services.AddScoped<ICommunityRankingService, CommunityRankingService>();
        services.AddScoped<IDiscussionService, DiscussionService>();
        
        // Configure WoW API with validation
        // The [Required] attributes on WowApiOptions handle validation via ValidateDataAnnotations()
        services.AddOptions<WowApiOptions>()
            .Bind(configuration.GetSection(WowApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart(); // Fail fast on startup
        
        services.AddHttpClient<WowApiService>();
        services.AddScoped<IWowApiService, WowApiService>();
        
        return services;
    }
}


