using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/meta-analysis")]
public class MetaAnalysisController(IMetaAnalysisService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetMetaAnalysis(
        [FromQuery] int? ratingMin = null,
        [FromQuery] int? ratingMax = null,
        [FromQuery] GameMode? gameMode = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var result = await service.GetMetaAnalysisAsync(
            ratingMin, ratingMax, gameMode, startDate, endDate, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("trends")]
    public async Task<ActionResult> GetTrends(
        [FromQuery] string composition,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(composition))
        {
            return BadRequest("composition parameter is required");
        }

        var result = await service.GetCompositionTrendsAsync(composition, days, ct);
        return Ok(result);
    }
}

