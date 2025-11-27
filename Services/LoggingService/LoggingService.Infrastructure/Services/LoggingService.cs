using LoggingService.Application.Abstractions;
using LoggingService.Core.DTOs;
using LoggingService.Core.Entities;
using LoggingService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Infrastructure.Services;

public class LoggingService(LoggingDbContext dbContext) : ILoggingService
{
    public async Task<LogEntryDto> LogAsync(CreateLogEntryDto dto, CancellationToken ct = default)
    {
        var log = new ApplicationLog
        {
            Timestamp = DateTime.UtcNow,
            Level = dto.Level,
            ServiceName = dto.ServiceName,
            Message = dto.Message,
            Exception = dto.Exception,
            UserId = dto.UserId,
            RequestPath = dto.RequestPath,
            RequestMethod = dto.RequestMethod,
            StatusCode = dto.StatusCode,
            Duration = dto.Duration,
            Properties = dto.Properties
        };

        dbContext.ApplicationLogs.Add(log);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(log);
    }

    public async Task<List<LogEntryDto>> GetLogsAsync(LogQueryDto query, CancellationToken ct = default)
    {
        var logsQuery = dbContext.ApplicationLogs.AsQueryable();

        if (!string.IsNullOrEmpty(query.ServiceName))
            logsQuery = logsQuery.Where(l => l.ServiceName == query.ServiceName);

        if (!string.IsNullOrEmpty(query.Level))
            logsQuery = logsQuery.Where(l => l.Level == query.Level);

        if (query.StartDate.HasValue)
            logsQuery = logsQuery.Where(l => l.Timestamp >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            logsQuery = logsQuery.Where(l => l.Timestamp <= query.EndDate.Value);

        if (query.UserId.HasValue)
            logsQuery = logsQuery.Where(l => l.UserId == query.UserId.Value);

        var logs = await logsQuery
            .OrderByDescending(l => l.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(ct);

        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<LogEntryDto>> GetLogsByServiceAsync(string serviceName, CancellationToken ct = default)
    {
        var logs = await dbContext.ApplicationLogs
            .Where(l => l.ServiceName == serviceName)
            .OrderByDescending(l => l.Timestamp)
            .Take(1000)
            .ToListAsync(ct);

        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<LogEntryDto>> GetLogsByLevelAsync(string level, CancellationToken ct = default)
    {
        var logs = await dbContext.ApplicationLogs
            .Where(l => l.Level == level)
            .OrderByDescending(l => l.Timestamp)
            .Take(1000)
            .ToListAsync(ct);

        return logs.Select(MapToDto).ToList();
    }

    public async Task<LogEntryDto?> GetLogByIdAsync(long id, CancellationToken ct = default)
    {
        var log = await dbContext.ApplicationLogs.FindAsync([id], ct);
        return log == null ? null : MapToDto(log);
    }

    private static LogEntryDto MapToDto(ApplicationLog log)
    {
        return new LogEntryDto
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            Level = log.Level,
            ServiceName = log.ServiceName,
            Message = log.Message,
            Exception = log.Exception,
            UserId = log.UserId,
            RequestPath = log.RequestPath,
            RequestMethod = log.RequestMethod,
            StatusCode = log.StatusCode,
            Duration = log.Duration,
            Properties = log.Properties
        };
    }
}

