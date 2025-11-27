using LoggingService.Core.DTOs;

namespace LoggingService.Application.Abstractions;

public interface ILoggingService
{
    Task<LogEntryDto> LogAsync(CreateLogEntryDto dto, CancellationToken ct = default);
    Task<LogQueryResultDto> GetLogsAsync(LogQueryDto query, CancellationToken ct = default);
    Task<LogQueryResultDto> GetLogsByServiceAsync(string serviceName, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<LogQueryResultDto> GetLogsByLevelAsync(string level, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<LogEntryDto?> GetLogByIdAsync(long id, CancellationToken ct = default);
}

