namespace LoggingService.Core.DTOs;

public class LogEntryDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Guid? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public double? Duration { get; set; }
    public string? Properties { get; set; }
}

public class CreateLogEntryDto
{
    public string Level { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Guid? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public double? Duration { get; set; }
    public string? Properties { get; set; }
}

public class LogQueryDto
{
    public string? ServiceName { get; set; }
    public string? Level { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
}

