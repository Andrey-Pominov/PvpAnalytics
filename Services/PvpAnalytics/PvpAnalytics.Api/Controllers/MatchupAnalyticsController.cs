using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/matchup-analytics")]
public class MatchupAnalyticsController(IMatchupAnalyticsService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetMatchup(
        [FromQuery] string class1,
        [FromQuery] string class2,
        [FromQuery] string? spec1 = null,
        [FromQuery] string? spec2 = null,
        [FromQuery] GameMode? gameMode = null,
        [FromQuery] int? ratingMin = null,
        [FromQuery] int? ratingMax = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(class1) || string.IsNullOrWhiteSpace(class2))
        {
            return BadRequest("class1 and class2 parameters are required");
        }

        var result = await service.GetMatchupAsync(
            class1, spec1, class2, spec2,
            gameMode, ratingMin, ratingMax,
            startDate, endDate, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/weaknesses")]
    public async Task<ActionResult> GetWeaknesses(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetPlayerMatchupSummaryAsync(playerId, ct);
        return Ok(result.Weaknesses);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/strengths")]
    public async Task<ActionResult> GetStrengths(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetPlayerMatchupSummaryAsync(playerId, ct);
        return Ok(result.Strengths);
    }

    [AllowAnonymous]
    [HttpGet("{playerId:long}/summary")]
    public async Task<ActionResult> GetSummary(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetPlayerMatchupSummaryAsync(playerId, ct);
        return Ok(result);
    }
}

