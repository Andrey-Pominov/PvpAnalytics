using LoggingService.Core.DTOs;

namespace LoggingService.Application.Abstractions;

public interface ILoggingService
{
    Task<LogEntryDto> LogAsync(CreateLogEntryDto dto, CancellationToken ct = default);
    Task<List<LogEntryDto>> GetLogsAsync(LogQueryDto query, CancellationToken ct = default);
    Task<List<LogEntryDto>> GetLogsByServiceAsync(string serviceName, CancellationToken ct = default);
    Task<List<LogEntryDto>> GetLogsByLevelAsync(string level, CancellationToken ct = default);
    Task<LogEntryDto?> GetLogByIdAsync(long id, CancellationToken ct = default);
}

