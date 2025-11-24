using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/session-analysis")]
public class SessionAnalysisController(ISessionAnalysisService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{playerId:long}")]
    public async Task<ActionResult> GetSessionAnalysis(
        long playerId,
        [FromQuery] int thresholdMinutes = 60,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var result = await service.GetSessionAnalysisAsync(
            playerId, thresholdMinutes, startDate, endDate, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/optimal-times")]
    public async Task<ActionResult> GetOptimalTimes(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetOptimalPlayTimesAsync(playerId, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/fatigue")]
    public async Task<ActionResult> GetFatigueAnalysis(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetFatigueAnalysisAsync(playerId, ct);
        return Ok(result);
    }
}

