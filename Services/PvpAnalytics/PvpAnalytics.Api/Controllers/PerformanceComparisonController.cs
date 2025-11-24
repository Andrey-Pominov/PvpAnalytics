using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/performance-comparison")]
public class PerformanceComparisonController(IPerformanceComparisonService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{playerId:long}")]
    public async Task<ActionResult> ComparePlayer(
        long playerId,
        [FromQuery] string spec,
        [FromQuery] int? ratingMin = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return BadRequest("spec parameter is required");
        }

        var result = await service.ComparePlayerAsync(playerId, spec, ratingMin, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/percentiles")]
    public async Task<ActionResult> GetPercentiles(
        long playerId,
        [FromQuery] string spec,
        [FromQuery] int? ratingMin = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return BadRequest("spec parameter is required");
        }

        var result = await service.GetPlayerPercentilesAsync(playerId, spec, ratingMin, ct);
        return Ok(result);
    }
}

