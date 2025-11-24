using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/team-composition")]
public class TeamCompositionController(ITeamCompositionService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("player/{playerId:long}/teams")]
    public async Task<ActionResult> GetPlayerTeams(long playerId, CancellationToken ct = default)
    {
        var result = await service.GetPlayerTeamsAsync(playerId, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("team/{composition}")]
    public async Task<ActionResult> GetTeamStats(string composition, CancellationToken ct = default)
    {
        var result = await service.GetTeamStatsAsync(composition, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("synergy/{player1Id:long}/{player2Id:long}")]
    public async Task<ActionResult> GetSynergy(long player1Id, long player2Id, CancellationToken ct = default)
    {
        var result = await service.GetPlayerSynergyAsync(player1Id, player2Id, ct);
        return result == null ? NotFound() : Ok(result);
    }
}

