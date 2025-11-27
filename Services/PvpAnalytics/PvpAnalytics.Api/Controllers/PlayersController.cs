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
    [HttpGet("{id:long}/stats")]
    public async Task<ActionResult<PlayerStatsDto>> GetStats(long id, CancellationToken ct)
    {
        var player = await service.GetAsync(id, ct);
        if (player is null)
        {
            return NotFound();
        }

        var joinedQuery = matchResultRepo.Query()
            .Where(mr => mr.PlayerId == id)
            .Join(
                matchRepo.Query(),
                mr => mr.MatchId,
                m => m.Id,
                (mr, m) => new { MatchResult = mr, Match = m }
            );

        var hasMatches = await joinedQuery.AnyAsync(ct);
        if (!hasMatches)
        {
            return Ok(new PlayerStatsDto
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                Realm = player.Realm,
                TotalMatches = 0,
                Wins = 0,
                Losses = 0,
                WinRate = 0.0,
                AverageMatchDuration = 0.0,
                FavoriteGameMode = null,
                FavoriteSpec = null,
            });
        }

        var statsQuery = joinedQuery
            .GroupBy(x => x.MatchResult.PlayerId)
            .Select(g => new
            {
                PlayerId = g.Key,
                TotalMatches = g.Count(),
                Wins = g.Count(x => x.MatchResult.IsWinner),
                AverageMatchDuration = g.Average(x => (double)x.Match.Duration),
            });

        var baseStats = await statsQuery.FirstOrDefaultAsync(ct);
        if (baseStats == null)
        {
            return Ok(new PlayerStatsDto
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                Realm = player.Realm,
                TotalMatches = 0,
                Wins = 0,
                Losses = 0,
                WinRate = 0.0,
                AverageMatchDuration = 0.0,
                FavoriteGameMode = null,
                FavoriteSpec = null,
            });
        }

        var favoriteGameMode = await joinedQuery
            .GroupBy(x => x.Match.GameMode)
            .Select(g => new { GameMode = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync(ct);

        var favoriteSpec = await matchResultRepo.Query()
            .Where(mr => mr.PlayerId == id && mr.Spec != null && mr.Spec != string.Empty)
            .GroupBy(mr => mr.Spec!)
            .Select(g => new { Spec = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync(ct);

        var stats = new PlayerStatsDto
        {
            PlayerId = baseStats.PlayerId,
            PlayerName = player.Name,
            Realm = player.Realm,
            TotalMatches = baseStats.TotalMatches,
            Wins = baseStats.Wins,
            Losses = baseStats.TotalMatches - baseStats.Wins,
            WinRate = baseStats.TotalMatches > 0 ? Math.Round(baseStats.Wins * 100.0 / baseStats.TotalMatches, 2) : 0.0,
            AverageMatchDuration = Math.Round(baseStats.AverageMatchDuration, 2),
            FavoriteGameMode = favoriteGameMode != null ? favoriteGameMode.GameMode.ToString() : null,
            FavoriteSpec = favoriteSpec?.Spec
        };

        return Ok(stats);
    }

    [AllowAnonymous]
    [HttpGet("{id:long}/matches")]
    public async Task<ActionResult<IEnumerable<PlayerMatchDto>>> GetMatches(
        long id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        if (skip < 0)
        {
            return BadRequest("skip must be greater than or equal to 0");
        }

        const int maxTake = 1000;
        if (take <= 0)
        {
            return BadRequest("take must be greater than 0");
        }

        if (take > maxTake)
        {
            return BadRequest($"take must not exceed {maxTake}");
        }

        var player = await service.GetAsync(id, ct);
        if (player is null)
        {
            return NotFound();
        }

        var matchesQuery = matchResultRepo.Query()
            .Where(mr => mr.PlayerId == id)
            .Join(
                matchRepo.Query(),
                mr => mr.MatchId,
                m => m.Id,
                (mr, m) => new
                {
                    MatchResult = mr,
                    Match = m
                }
            )
            .OrderByDescending(x => x.Match.CreatedOn)
            .Skip(skip)
            .Take(take)
            .Select(x => new PlayerMatchDto
            {
                MatchId = x.Match.Id,
                CreatedOn = x.Match.CreatedOn,
                MapName = x.Match.MapName,
                GameMode = x.Match.GameMode.ToString(),
                Duration = x.Match.Duration,
                IsWinner = x.MatchResult.IsWinner,
                RatingBefore = x.MatchResult.RatingBefore,
                RatingAfter = x.MatchResult.RatingAfter,
                RatingChange = x.MatchResult.RatingAfter - x.MatchResult.RatingBefore,
                Spec = x.MatchResult.Spec,
                Team = x.MatchResult.Team
            });

        var matches = await matchesQuery.ToListAsync(ct);

        return Ok(matches);
    }

    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Player>> Get(long id, CancellationToken ct)
    {
        var entity = await service.GetAsync(id, ct);
        return entity is null ? NotFound() : Ok(entity);
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