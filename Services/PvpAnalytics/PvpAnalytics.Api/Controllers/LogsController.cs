using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Api.Controllers;

//[Authorize] // Commented out to allow unauthenticated uploads
[ApiController]
[Route("api/[controller]")]
public class LogsController(ICombatLogIngestionService ingestion, ILogger<LogsController> logger) : ControllerBase
{
    /// <summary>
    /// Ingests an uploaded log file and returns all matches found in the file.
    /// Processes multiple arena matches: starts recording on ARENA_MATCH_START, stops on ZONE_CHANGE.
    /// </summary>
    /// <param name="file">The uploaded log file to ingest; if null or empty a BadRequest is returned.</param>
    /// <returns>
    /// An ActionResult containing the list of persisted matches:
    /// - `200 OK` with list of matches when ingestion completed successfully, 
    /// - `400 BadRequest` when no file was provided.
    /// </returns>
    [HttpPost("upload")]
    [RequestSizeLimit(104857600)] // 100 MB
    public async Task<ActionResult<List<Match>>> Upload([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            logger.LogWarning("Combat log upload rejected: no file provided.");
            return BadRequest("No file provided");
        }

        logger.LogInformation("Starting combat log ingestion for file {FileName} ({FileSizeBytes} bytes).",
            file.FileName, file.Length);

        try
        {
            await using var stream = file.OpenReadStream();
            var matches = await ingestion.IngestAsync(stream, ct);

            logger.LogInformation("Combat log ingestion completed. Processed {MatchCount} match(es).", matches.Count);

            if (matches.Count > 0)
            {
                var firstMatch = matches[0];
                if (firstMatch.Id > 0)
                {
                    Response.Headers.Append("X-Match-Count", matches.Count.ToString());
                    return Ok(matches);
                }
            }

            return Ok(matches);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(
                "Combat log ingestion cancelled for file {FileName}. See inner exception for details. {ex} ",
                file.FileName, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Combat log ingestion failed for file {FileName}.", file.FileName);
            // Rethrow with additional context so callers have more information than the raw exception.
            throw new InvalidOperationException(
                $"Combat log ingestion failed for file '{file.FileName}'. See inner exception for details.",
                ex);
        }
    }
}