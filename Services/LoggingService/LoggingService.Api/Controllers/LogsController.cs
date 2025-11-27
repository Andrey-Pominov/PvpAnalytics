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
    public async Task<ActionResult<LogQueryResultDto>> GetLogs([FromQuery] LogQueryDto query, CancellationToken ct)
    {
        var result = await loggingService.GetLogsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LogEntryDto>> GetLog(long id, CancellationToken ct)
    {
        var log = await loggingService.GetLogByIdAsync(id, ct);
        return log == null ? NotFound() : Ok(log);
    }

    [HttpGet("service/{serviceName}")]
    public async Task<ActionResult<LogQueryResultDto>> GetLogsByService(
        string serviceName, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 100, 
        CancellationToken ct = default)
    {
        var result = await loggingService.GetLogsByServiceAsync(serviceName, skip, take, ct);
        return Ok(result);
    }

    [HttpGet("level/{level}")]
    public async Task<ActionResult<LogQueryResultDto>> GetLogsByLevel(
        string level, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 100, 
        CancellationToken ct = default)
    {
        var result = await loggingService.GetLogsByLevelAsync(level, skip, take, ct);
        return Ok(result);
    }
}

