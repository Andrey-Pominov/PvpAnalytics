using LoggingService.Application.Abstractions;
using LoggingService.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoggingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController(ILoggingService loggingService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<LogEntryDto>> CreateLog([FromBody] CreateLogEntryDto dto, CancellationToken ct)
    {
        var log = await loggingService.LogAsync(dto, ct);
        return Ok(log);
    }

    [HttpGet]
    public async Task<ActionResult<List<LogEntryDto>>> GetLogs([FromQuery] LogQueryDto query, CancellationToken ct)
    {
        var logs = await loggingService.GetLogsAsync(query, ct);
        return Ok(logs);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LogEntryDto>> GetLog(long id, CancellationToken ct)
    {
        var log = await loggingService.GetLogByIdAsync(id, ct);
        return log == null ? NotFound() : Ok(log);
    }

    [HttpGet("service/{serviceName}")]
    public async Task<ActionResult<List<LogEntryDto>>> GetLogsByService(string serviceName, CancellationToken ct)
    {
        var logs = await loggingService.GetLogsByServiceAsync(serviceName, ct);
        return Ok(logs);
    }

    [HttpGet("level/{level}")]
    public async Task<ActionResult<List<LogEntryDto>>> GetLogsByLevel(string level, CancellationToken ct)
    {
        var logs = await loggingService.GetLogsByLevelAsync(level, ct);
        return Ok(logs);
    }
}

