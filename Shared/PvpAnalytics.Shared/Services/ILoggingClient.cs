namespace PvpAnalytics.Shared.Services;

public interface ILoggingClient : IDisposable
{
    Task RegisterServiceAsync(string serviceName, string endpoint, string version, CancellationToken ct = default);
    Task SendHeartbeatAsync(string serviceName, CancellationToken ct = default);
    void StartHeartbeat(string serviceName, TimeSpan interval);
}

