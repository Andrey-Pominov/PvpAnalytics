namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Factory for resolving the appropriate combat log ingestion service based on file format.
/// </summary>
public interface ICombatLogIngestionServiceFactory
{
    /// <summary>
    /// Gets the appropriate ingestion service for the specified format.
    /// </summary>
    /// <param name="format">The combat log format.</param>
    /// <returns>The ingestion service for the specified format.</returns>
    ICombatLogIngestionService GetService(CombatLogFormat format);
}

