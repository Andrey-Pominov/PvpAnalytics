using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Level is required")]
    [StringLength(50, ErrorMessage = "Level must not exceed 50 characters")]
    [RegularExpression("^(Trace|Debug|Information|Warning|Error|Critical)$", ErrorMessage = "Level must be a valid log level")]
    public string Level { get; set; } = string.Empty;

    [Required(ErrorMessage = "ServiceName is required")]
    [StringLength(100, ErrorMessage = "ServiceName must not exceed 100 characters")]
    public string ServiceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [StringLength(10000, ErrorMessage = "Message must not exceed 10000 characters")]
    public string Message { get; set; } = string.Empty;

    [StringLength(50000, ErrorMessage = "Exception must not exceed 50000 characters")]
    public string? Exception { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(2000, ErrorMessage = "RequestPath must not exceed 2000 characters")]
    public string? RequestPath { get; set; }

    [StringLength(10, ErrorMessage = "RequestMethod must not exceed 10 characters")]
    [RegularExpression("^(GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS)$", ErrorMessage = "RequestMethod must be a valid HTTP method")]
    public string? RequestMethod { get; set; }

    [Range(100, 599, ErrorMessage = "StatusCode must be between 100 and 599")]
    public int? StatusCode { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Duration must be non-negative")]
    public double? Duration { get; set; }

    [StringLength(100000, ErrorMessage = "Properties must not exceed 100000 characters")]
    public string? Properties { get; set; }
}

public class LogQueryDto : IValidatableObject
{
    [MaxLength(100, ErrorMessage = "ServiceName must not exceed 100 characters")]
    public string? ServiceName { get; set; }

    [RegularExpression("^(Trace|Debug|Information|Warning|Error|Critical)$", ErrorMessage = "Level must be a valid log level (Trace, Debug, Information, Warning, Error, or Critical)")]
    public string? Level { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? UserId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Skip must be non-negative")]
    public int Skip { get; set; } = 0;

    [Range(1, 1000, ErrorMessage = "Take must be between 1 and 1000")]
    public int Take { get; set; } = 100;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
        {
            yield return new ValidationResult(
                "StartDate must be less than or equal to EndDate",
                new[] { nameof(StartDate), nameof(EndDate) });
        }
    }
}

public class LogQueryResultDto
{
    public List<LogEntryDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
}

