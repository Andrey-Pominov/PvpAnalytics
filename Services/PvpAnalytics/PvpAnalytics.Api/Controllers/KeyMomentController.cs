using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/key-moments")]
public class KeyMomentController(IKeyMomentService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("match/{matchId:long}")]
    public async Task<ActionResult> GetMatchKeyMoments(long matchId, CancellationToken ct = default)
    {
        var result = await service.GetKeyMomentsForMatchAsync(matchId, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("player/{playerId:long}/recent")]
    public async Task<ActionResult> GetRecentKeyMoments(
        long playerId,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await service.GetRecentKeyMomentsAsync(playerId, limit, ct);
        return Ok(result);
    }
}

