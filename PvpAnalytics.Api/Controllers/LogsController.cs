using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogsController(ICombatLogIngestionService ingestion, ILogger<LogsController> logger) : ControllerBase
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
        if (file == null || file.Length == 0)
        {
            logger.LogWarning("Combat log upload rejected: no file provided.");
            return BadRequest("No file provided");
        }

        logger.LogInformation("Starting combat log ingestion for file {FileName} ({FileSizeBytes} bytes).", file.FileName, file.Length);

        try
        {
            await using var stream = file.OpenReadStream();
            var match = await ingestion.IngestAsync(stream, ct);
            if (match.Id > 0)
            {
                logger.LogInformation("Combat log ingestion persisted match {MatchId} on map {MapName}.", match.Id, match.MapName);
                return CreatedAtAction("Get", "Matches", new { id = match.Id }, match);
            }

            logger.LogInformation("Combat log ingestion completed without persistence (Id=0). Returning Accepted.");
            return Accepted(match);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Combat log ingestion cancelled for file {FileName}.", file.FileName);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Combat log ingestion failed for file {FileName}.", file.FileName);
            throw;
        }
    }
}

