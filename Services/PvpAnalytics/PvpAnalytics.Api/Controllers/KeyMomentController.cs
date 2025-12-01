using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.DTOs;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/key-moments")]
public class KeyMomentController(IKeyMomentService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("match/{matchId:long}")]
    public async Task<ActionResult> GetMatchKeyMoments(long matchId, CancellationToken ct = default)
    {
        try
        {
            var result = await service.GetKeyMomentsForMatchAsync(matchId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception for observability
            // TODO: Inject ILogger and log here
            // _logger.LogError(ex, "Failed to retrieve key moments for match {MatchId}", matchId);

            // Return appropriate status codes based on exception type.
            // For now, let the global exception handler manage it.
            throw;
        }
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

