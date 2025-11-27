namespace LoggingService.Core.Entities;

public class RegisteredService
{
    public long Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public string Status { get; set; } = "Online";
}

