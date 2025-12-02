namespace PvpAnalytics.Shared.Services;

public interface ILoggingClient : IDisposable
{
    Task LogAsync(string level, string message, string? exception = null, Guid? userId = null, 
        string? requestPath = null, string? requestMethod = null, int? statusCode = null, 
        double? duration = null, string? properties = null, CancellationToken ct = default);
    
    Task RegisterServiceAsync(string serviceName, string endpoint, string version, CancellationToken ct = default);
    Task SendHeartbeatAsync(string serviceName, CancellationToken ct = default);
    void StartHeartbeat(string serviceName, TimeSpan interval);
}

