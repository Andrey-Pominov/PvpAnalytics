using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController(ICombatLogIngestionService ingestion) : ControllerBase
{
    /// <summary>
    /// Ingests an uploaded log file and returns the resulting Match resource.
    /// </summary>
    /// <param name="file">The uploaded log file to ingest; if null or empty a BadRequest is returned.</param>
    /// <returns>
    /// An ActionResult containing the created or accepted Match:
    /// - `201 Created` with a Location header pointing to GET /api/matches/{id} when the match was persisted (`match.Id > 0`), 
    /// - `202 Accepted` with the match when ingestion completed but no persistent id was assigned, 
    /// - `400 BadRequest` when no file was provided.
    /// </returns>
    [HttpPost("upload")]
    [RequestSizeLimit(104857600)] // 100 MB
    public async Task<ActionResult<Match>> Upload([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file provided");
        await using var stream = file.OpenReadStream();
        var match = await ingestion.IngestAsync(stream, ct);
        if (match.Id > 0)
        {
            // Point Location header to GET /api/matches/{id}
            return CreatedAtAction("Get", "Matches", new { id = match.Id }, match);
        }
        
        return Accepted(match);
    }
}

