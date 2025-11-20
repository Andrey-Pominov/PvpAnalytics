using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.Entities;

namespace PvpAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(ICrudService<Player> service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Player>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [Authorize]
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