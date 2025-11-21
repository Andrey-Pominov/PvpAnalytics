using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(
    ICrudService<Player> service,
    IRepository<MatchResult> matchResultRepo,
    IRepository<Match> matchRepo) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Player>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Player>> Get(long id, CancellationToken ct)
    {
        var entity = await service.GetAsync(id, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    [AllowAnonymous]
    [HttpGet("{id:long}/stats")]
    public async Task<ActionResult<PlayerStatsDto>> GetStats(long id, CancellationToken ct)
    {
        var player = await service.GetAsync(id, ct);
        if (player is null)
        {
            return NotFound();
        }

        var matchResults = await matchResultRepo.ListAsync(mr => mr.PlayerId == id, ct);
        var totalMatches = matchResults.Count;
        var wins = matchResults.Count(mr => mr.IsWinner);
        var losses = totalMatches - wins;
        var winRate = totalMatches > 0 ? (double)wins / totalMatches * 100 : 0;

        // Get match durations
        var matchIds = matchResults.Select(mr => mr.MatchId).ToList();
        var matches = await matchRepo.ListAsync(m => matchIds.Contains(m.Id), ct);
        var averageDuration = matches.Any() ? matches.Average(m => m.Duration) : 0;

        // Find favorite game mode
        var gameModeGroups = matches
            .GroupBy(m => m.GameMode)
            .OrderByDescending(g => g.Count())
            .ToList();
        var favoriteGameMode = gameModeGroups.FirstOrDefault()?.Key;

        // Find favorite spec
        var specGroups = matchResults
            .Where(mr => !string.IsNullOrWhiteSpace(mr.Spec))
            .GroupBy(mr => mr.Spec)
            .OrderByDescending(g => g.Count())
            .ToList();
        var favoriteSpec = specGroups.FirstOrDefault()?.Key;

        var stats = new PlayerStatsDto
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Realm = player.Realm,
            TotalMatches = totalMatches,
            Wins = wins,
            Losses = losses,
            WinRate = Math.Round(winRate, 2),
            AverageMatchDuration = Math.Round(averageDuration, 2),
            FavoriteGameMode = favoriteGameMode,
            FavoriteSpec = favoriteSpec
        };

        return Ok(stats);
    }

    [AllowAnonymous]
    [HttpGet("{id:long}/matches")]
    public async Task<ActionResult<IEnumerable<PlayerMatchDto>>> GetMatches(long id, CancellationToken ct)
    {
        var player = await service.GetAsync(id, ct);
        if (player is null)
        {
            return NotFound();
        }

        var matchResults = await matchResultRepo.ListAsync(mr => mr.PlayerId == id, ct);
        var matchIds = matchResults.Select(mr => mr.MatchId).ToList();
        var matches = await matchRepo.ListAsync(m => matchIds.Contains(m.Id), ct);

        var matchDict = matches.ToDictionary(m => m.Id);
        var resultDict = matchResults.ToDictionary(mr => mr.MatchId);

        var playerMatches = matches
            .OrderByDescending(m => m.CreatedOn)
            .Select(m =>
            {
                var result = resultDict.GetValueOrDefault(m.Id);
                return new PlayerMatchDto
                {
                    MatchId = m.Id,
                    CreatedOn = m.CreatedOn,
                    MapName = m.MapName,
                    ArenaZone = (int)m.ArenaZone,
                    GameMode = m.GameMode,
                    Duration = m.Duration,
                    IsRanked = m.IsRanked,
                    IsWinner = result?.IsWinner ?? false,
                    RatingBefore = result?.RatingBefore ?? 0,
                    RatingAfter = result?.RatingAfter ?? 0,
                    Spec = result?.Spec
                };
            })
            .ToList();

        return Ok(playerMatches);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Player>> Create([FromBody] Player entity, CancellationToken ct)
    {
        var created = await service.CreateAsync(entity, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Player entity, CancellationToken ct)
    {
        if (entity.Id == 0) entity.Id = id;
        if (entity.Id != id) return BadRequest("Mismatched id");
        var existing = await service.GetAsync(id, ct);
        if (existing is null) return NotFound();
        await service.UpdateAsync(entity, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var existing = await service.GetAsync(id, ct);
        if (existing is null) return NotFound();
        await service.DeleteAsync(existing, ct);
        return NoContent();
    }
}