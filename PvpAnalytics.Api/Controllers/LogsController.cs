using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController(ICombatLogIngestionService ingestion) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(104857600)] // 100 MB
    public async Task<ActionResult<Match>> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file provided");
        await using var stream = file.OpenReadStream();
        var match = await ingestion.IngestAsync(stream, ct);
        if (match.Id > 0)
        {
            // Point Location header to GET /api/matches/{id}
            return CreatedAtAction("Get", "Matches", new { id = match.Id }, match);
        }
        // No match persisted: return 202 Accepted with body
        return Accepted(match);
    }
}


