using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Application.Logs;

public interface ICombatLogIngestionService
{
    Task<Match> IngestAsync(Stream fileStream, CancellationToken ct = default);
}


