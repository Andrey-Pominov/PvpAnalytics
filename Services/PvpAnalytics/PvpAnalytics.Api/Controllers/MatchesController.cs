using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController(ICrudService<Match> service, IMatchDetailService detailService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Match>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [AllowAnonymous]
    [HttpGet("{id:long}/detail")]
    public async Task<ActionResult<MatchDetailDto>> GetDetail(long id, CancellationToken ct)
    {
        var detail = await detailService.GetMatchDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [Authorize]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Match>> Get(long id, CancellationToken ct)
    {
        var entity = await service.GetAsync(id, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Match>> Create([FromBody] Match entity, CancellationToken ct)
    {
        var created = await service.CreateAsync(entity, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Match entity, CancellationToken ct)
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
