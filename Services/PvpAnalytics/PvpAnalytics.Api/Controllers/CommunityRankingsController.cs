using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/community-rankings")]
public class CommunityRankingsController(ICommunityRankingService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{rankingType}")]
    public async Task<ActionResult> GetRankings(
        string rankingType,
        [FromQuery] string period = "weekly",
        [FromQuery] string? scope = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var rankings = await service.GetRankingsAsync(rankingType, period, scope, limit, ct);
        return Ok(rankings);
    }
}

