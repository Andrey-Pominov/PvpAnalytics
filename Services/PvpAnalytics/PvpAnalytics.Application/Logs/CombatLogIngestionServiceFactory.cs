using Microsoft.Extensions.DependencyInjection;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Factory implementation that resolves combat log ingestion services using keyed services.
/// </summary>
public class CombatLogIngestionServiceFactory(IServiceProvider serviceProvider) : ICombatLogIngestionServiceFactory
{
    public ICombatLogIngestionService GetService(CombatLogFormat format)
    {
        return serviceProvider.GetRequiredKeyedService<ICombatLogIngestionService>(format);
    }
}

