using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Application.Logs;

public interface ICombatLogIngestionService
{
    Task<List<Match>> IngestAsync(Stream fileStream, CancellationToken ct = default);
}


