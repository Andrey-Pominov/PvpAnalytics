using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/highlights")]
public class HighlightsController(IHighlightsService service) : ControllerBase
{
    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetHighlights(
        [FromQuery] string period = "day",
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var highlights = await service.GetHighlightsAsync(period, limit, ct);
        return Ok(highlights);
    }

    [Authorize]
    [HttpPost("{matchId:long}")]
    public async Task<ActionResult> FeatureMatch(
        long matchId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        var featured = await service.FeatureMatchAsync(matchId, reason, userId, ct);
        return featured == null ? BadRequest("Match not found or already featured") : Ok(featured);
    }
}

