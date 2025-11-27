using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LoggingService.Application.Abstractions;
using LoggingService.Core.DTOs;
using PvpAnalytics.Shared.Protos;
using LoggingServiceBase = PvpAnalytics.Shared.Protos.LoggingService.LoggingServiceBase;

namespace LoggingService.Infrastructure.Services;

public class LoggingGrpcService(
    ILoggingService loggingService,
    IServiceRegistry serviceRegistry) : LoggingServiceBase
{
    private static Guid? TryParseGuid(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Guid.TryParse(value, out var guid))
            return guid;

        throw new RpcException(new Status(
            StatusCode.InvalidArgument,
            $"Invalid GUID format for parameter '{parameterName}': '{value}'. Expected a valid GUID format."));
    }

    private static DateTime? TryParseDateTime(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, out var dateTime))
            return dateTime;

        throw new RpcException(new Status(
            StatusCode.InvalidArgument,
            $"Invalid date format for parameter '{parameterName}': '{value}'. Expected a valid date/time format."));
    }
    public override async Task<LogEntryResponse> CreateLog(CreateLogRequest request, ServerCallContext context)
    {
        var dto = new CreateLogEntryDto
        {
            Level = request.Level,
            ServiceName = request.ServiceName,
            Message = request.Message,
            Exception = string.IsNullOrEmpty(request.Exception) ? null : request.Exception,
            UserId = TryParseGuid(request.UserId, nameof(request.UserId)),
            RequestPath = string.IsNullOrEmpty(request.RequestPath) ? null : request.RequestPath,
            RequestMethod = string.IsNullOrEmpty(request.RequestMethod) ? null : request.RequestMethod,
            StatusCode = request.StatusCode == 0 ? null : request.StatusCode,
            Duration = request.Duration == 0 ? null : request.Duration,
            Properties = request.Properties != null && request.Properties.Fields.Count > 0 
                ? Google.Protobuf.JsonFormatter.Default.Format(request.Properties)
                : null
        };

        var log = await loggingService.LogAsync(dto, context.CancellationToken);

        return new LogEntryResponse
        {
            Id = log.Id,
            Timestamp = Timestamp.FromDateTime(log.Timestamp.ToUniversalTime()),
            Level = log.Level,
            ServiceName = log.ServiceName,
            Message = log.Message,
            Exception = log.Exception ?? string.Empty,
            UserId = log.UserId?.ToString() ?? string.Empty,
            RequestPath = log.RequestPath ?? string.Empty,
            RequestMethod = log.RequestMethod ?? string.Empty,
            StatusCode = log.StatusCode ?? 0,
            Duration = log.Duration ?? 0,
            Properties = !string.IsNullOrEmpty(log.Properties) 
                ? Struct.Parser.ParseJson(log.Properties)
                : new Struct()
        };
    }

    public override async Task<LogQueryResponse> GetLogs(LogQueryRequest request, ServerCallContext context)
    {
        var query = new LogQueryDto
        {
            ServiceName = string.IsNullOrEmpty(request.ServiceName) ? null : request.ServiceName,
            Level = string.IsNullOrEmpty(request.Level) ? null : request.Level,
            StartDate = request.StartDate != null ? request.StartDate.ToDateTime() : null,
            EndDate = request.EndDate != null ? request.EndDate.ToDateTime() : null,
            UserId = TryParseGuid(request.UserId, nameof(request.UserId)),
            Skip = request.Skip,
            Take = request.Take
        };

        var logs = await loggingService.GetLogsAsync(query, context.CancellationToken);

        var response = new LogQueryResponse
        {
            TotalCount = logs.Count
        };

        response.Logs.AddRange(logs.Select(log => new LogEntryResponse
        {
            Id = log.Id,
            Timestamp = Timestamp.FromDateTime(log.Timestamp.ToUniversalTime()),
            Level = log.Level,
            ServiceName = log.ServiceName,
            Message = log.Message,
            Exception = log.Exception ?? string.Empty,
            UserId = log.UserId?.ToString() ?? string.Empty,
            RequestPath = log.RequestPath ?? string.Empty,
            RequestMethod = log.RequestMethod ?? string.Empty,
            StatusCode = log.StatusCode ?? 0,
            Duration = log.Duration ?? 0,
            Properties = !string.IsNullOrEmpty(log.Properties) 
                ? Struct.Parser.ParseJson(log.Properties)
                : new Struct()
        }));

        return response;
    }

    public override async Task<LogEntryResponse> GetLogById(LogByIdRequest request, ServerCallContext context)
    {
        var log = await loggingService.GetLogByIdAsync(request.Id, context.CancellationToken);

        if (log == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Log with ID {request.Id} not found"));

        return new LogEntryResponse
        {
            Id = log.Id,
            Timestamp = Timestamp.FromDateTime(log.Timestamp.ToUniversalTime()),
            Level = log.Level,
            ServiceName = log.ServiceName,
            Message = log.Message,
            Exception = log.Exception ?? string.Empty,
            UserId = log.UserId?.ToString() ?? string.Empty,
            RequestPath = log.RequestPath ?? string.Empty,
            RequestMethod = log.RequestMethod ?? string.Empty,
            StatusCode = log.StatusCode ?? 0,
            Duration = log.Duration ?? 0,
            Properties = !string.IsNullOrEmpty(log.Properties) 
                ? Struct.Parser.ParseJson(log.Properties)
                : new Struct()
        };
    }

    public override async Task<LogQueryResponse> GetLogsByService(LogsByServiceRequest request, ServerCallContext context)
    {
        var logs = await loggingService.GetLogsByServiceAsync(request.ServiceName, context.CancellationToken);

        var response = new LogQueryResponse
        {
            TotalCount = logs.Count
        };

        response.Logs.AddRange(logs.Select(log => new LogEntryResponse
        {
            Id = log.Id,
            Timestamp = Timestamp.FromDateTime(log.Timestamp.ToUniversalTime()),
            Level = log.Level,
            ServiceName = log.ServiceName,
            Message = log.Message,
            Exception = log.Exception ?? string.Empty,
            UserId = log.UserId?.ToString() ?? string.Empty,
            RequestPath = log.RequestPath ?? string.Empty,
            RequestMethod = log.RequestMethod ?? string.Empty,
            StatusCode = log.StatusCode ?? 0,
            Duration = log.Duration ?? 0,
            Properties = !string.IsNullOrEmpty(log.Properties) 
                ? Struct.Parser.ParseJson(log.Properties)
                : new Struct()
        }));

        return response;
    }

    public override async Task<LogQueryResponse> GetLogsByLevel(LogsByLevelRequest request, ServerCallContext context)
    {
        var logs = await loggingService.GetLogsByLevelAsync(request.Level, context.CancellationToken);

        var response = new LogQueryResponse
        {
            TotalCount = logs.Count
        };

        response.Logs.AddRange(logs.Select(log => new LogEntryResponse
        {
            Id = log.Id,
            Timestamp = Timestamp.FromDateTime(log.Timestamp.ToUniversalTime()),
            Level = log.Level,
            ServiceName = log.ServiceName,
            Message = log.Message,
            Exception = log.Exception ?? string.Empty,
            UserId = log.UserId?.ToString() ?? string.Empty,
            RequestPath = log.RequestPath ?? string.Empty,
            RequestMethod = log.RequestMethod ?? string.Empty,
            StatusCode = log.StatusCode ?? 0,
            Duration = log.Duration ?? 0,
            Properties = !string.IsNullOrEmpty(log.Properties) 
                ? Struct.Parser.ParseJson(log.Properties)
                : new Struct()
        }));

        return response;
    }

    public override async Task<ServiceRegistrationResponse> RegisterService(ServiceRegistrationRequest request, ServerCallContext context)
    {
        var serviceId = await serviceRegistry.RegisterServiceAsync(
            request.ServiceName,
            request.Endpoint,
            request.Version,
            context.CancellationToken);

        return new ServiceRegistrationResponse
        {
            Success = true,
            Message = "Service registered successfully",
            ServiceId = serviceId
        };
    }

    public override async Task<HeartbeatResponse> Heartbeat(HeartbeatRequest request, ServerCallContext context)
    {
        var success = await serviceRegistry.UpdateHeartbeatAsync(request.ServiceName, context.CancellationToken);

        return new HeartbeatResponse
        {
            Success = success,
            Message = success ? "Heartbeat updated" : "Service not found"
        };
    }
}

