using Grpc.Core;
using LoggingService.Core.DTOs;

namespace LoggingService.GrpcServices;

public class LogService : LogService.LogServiceBase
{
    private readonly ILogger<LogService> _logger;

    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
    }

    public override Task<LogResponse> LogEvent(LogRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received log event from {ServiceName}: {Message}", 
            request.ServiceName, request.Message);
        
        // Process log event (save to database, etc.)
        return Task.FromResult(new LogResponse
        {
            Success = true,
            MessageId = Guid.NewGuid().ToString()
        });
    }

    public override Task<HeartbeatResponse> Heartbeat(HeartbeatRequest request, ServerCallContext context)
    {
        _logger.LogDebug("Heartbeat from {ServiceName}", request.ServiceName);
        
        return Task.FromResult(new HeartbeatResponse
        {
            Acknowledged = true,
            Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
        });
    }
}
