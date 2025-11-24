using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/rating-progression")]
public class RatingProgressionController(IRatingProgressionService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{playerId:long}")]
    public async Task<ActionResult> GetProgression(
        long playerId,
        [FromQuery] GameMode? gameMode = null,
        [FromQuery] string? spec = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var result = await service.GetRatingProgressionAsync(
            playerId, gameMode, spec, startDate, endDate, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/summary")]
    public async Task<ActionResult> GetSummary(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetRatingSummaryAsync(playerId, ct);
        return Ok(result);
    }
}

