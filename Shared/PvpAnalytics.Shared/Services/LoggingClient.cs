using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PvpAnalytics.Shared.Protos;
using System.Diagnostics;

namespace PvpAnalytics.Shared.Services;

public class LoggingClient : ILoggingClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly LoggingService.LoggingServiceClient _client;
    private readonly string _serviceName;
    private readonly ILogger<LoggingClient>? _logger;
    private Timer? _heartbeatTimer;
    private bool _disposed;

    public LoggingClient(IConfiguration configuration, ILogger<LoggingClient>? logger = null)
    {
        var endpoint = configuration["LoggingService:GrpcEndpoint"] 
            ?? throw new InvalidOperationException("LoggingService:GrpcEndpoint not configured");
        
        _serviceName = configuration["LoggingService:ServiceName"] 
            ?? throw new InvalidOperationException("LoggingService:ServiceName not configured");
        
        _logger = logger;
        _channel = GrpcChannel.ForAddress(endpoint);
        _client = new LoggingService.LoggingServiceClient(_channel);
    }

    public async Task LogAsync(string level, string message, string? exception = null, Guid? userId = null,
        string? requestPath = null, string? requestMethod = null, int? statusCode = null,
        double? duration = null, string? properties = null, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateLogRequest
            {
                Level = level,
                ServiceName = _serviceName,
                Message = message,
                Exception = exception ?? string.Empty,
                UserId = userId?.ToString() ?? string.Empty,
                RequestPath = requestPath ?? string.Empty,
                RequestMethod = requestMethod ?? string.Empty,
                StatusCode = statusCode ?? 0,
                Duration = duration ?? 0,
                Properties = properties ?? string.Empty
            };

            await _client.CreateLogAsync(request, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send log to LoggingService");
        }
    }

    public async Task RegisterServiceAsync(string serviceName, string endpoint, string version, CancellationToken ct = default)
    {
        try
        {
            var request = new ServiceRegistrationRequest
            {
                ServiceName = serviceName,
                Endpoint = endpoint,
                Version = version
            };

            var response = await _client.RegisterServiceAsync(request, cancellationToken: ct);
            _logger?.LogInformation("Service {ServiceName} registered with LoggingService: {Message}", 
                serviceName, response.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register service {ServiceName} with LoggingService", serviceName);
        }
    }

    public async Task SendHeartbeatAsync(string serviceName, CancellationToken ct = default)
    {
        try
        {
            var request = new HeartbeatRequest
            {
                ServiceName = serviceName
            };

            await _client.HeartbeatAsync(request, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send heartbeat for service {ServiceName}", serviceName);
        }
    }

    public void StartHeartbeat(string serviceName, TimeSpan interval)
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = new Timer(async _ =>
        {
            await SendHeartbeatAsync(serviceName);
        }, null, TimeSpan.Zero, interval);
    }

    public void StopHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        StopHeartbeat();
        _channel?.Dispose();
        _disposed = true;
    }
}

