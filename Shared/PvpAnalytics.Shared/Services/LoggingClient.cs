using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PvpAnalytics.Shared.Protos;
using System.Diagnostics;
using System.Threading;

namespace PvpAnalytics.Shared.Services;

public class LoggingClient : ILoggingClient
{
    private readonly GrpcChannel _channel;
    private readonly LoggingService.LoggingServiceClient _client;
    private readonly string _serviceName;
    private readonly ILogger<LoggingClient>? _logger;
    private readonly object _timerLock = new object();
    private Timer? _heartbeatTimer;
    private CancellationTokenSource? _heartbeatCts;
    private int _disposed; // 0 = false, 1 = true (for Interlocked operations)

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
                Properties = !string.IsNullOrEmpty(properties) 
                    ? Struct.Parser.ParseJson(properties)
                    : new Struct()
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
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(LoggingClient));

        Timer? oldTimer = null;
        CancellationTokenSource? oldCts = null;
        lock (_timerLock)
        {
            // Atomically swap out the old timer and cancellation token source
            oldTimer = Interlocked.Exchange(ref _heartbeatTimer, null);
            oldCts = Interlocked.Exchange(ref _heartbeatCts, null);
            
            // Create new cancellation token source for this heartbeat session
            var newCts = new CancellationTokenSource();
            _heartbeatCts = newCts;
            
            // Create new timer with a safe callback wrapper
            _heartbeatTimer = new Timer(_ =>
            {
                // Check if cancellation was requested (timer was stopped/disposed)
                if (newCts.Token.IsCancellationRequested)
                    return;
                
                // Use Task.Run to handle async operation safely
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Check again inside the async context
                        if (!newCts.Token.IsCancellationRequested)
                        {
                            await SendHeartbeatAsync(serviceName, newCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelled, ignore
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in heartbeat callback for service {ServiceName}", serviceName);
                    }
                }, newCts.Token);
            }, null, TimeSpan.Zero, interval);
        }
        
        // Dispose old timer and cancel old CTS outside the lock
        oldCts?.Cancel();
        oldCts?.Dispose();
        oldTimer?.Dispose();
    }

    public void StopHeartbeat()
    {
        Timer? timerToDispose = null;
        CancellationTokenSource? ctsToDispose = null;
        lock (_timerLock)
        {
            // Atomically swap out the timer and cancellation token source
            timerToDispose = Interlocked.Exchange(ref _heartbeatTimer, null);
            ctsToDispose = Interlocked.Exchange(ref _heartbeatCts, null);
        }
        
        // Cancel and dispose outside the lock
        if (ctsToDispose != null)
        {
            ctsToDispose.Cancel();
            ctsToDispose.Dispose();
        }
        timerToDispose?.Dispose();
    }

    public void Dispose()
    {
        // Use Interlocked to atomically check and set disposed flag
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;
        
        StopHeartbeat();
        _channel?.Dispose();
    }
}

