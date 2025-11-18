using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Services;
using PaymentService.Core.Entities;

namespace PaymentService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentController(ICrudService<Payment> service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Payment>> Get(long id, CancellationToken ct)
    {
        var entity = await service.GetAsync(id, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> Create([FromBody] Payment entity, CancellationToken ct)
    {
        if (entity.Id != 0)
        {
            return BadRequest("Id should not be set when creating a new payment.");
        }
        
        entity.CreatedAt = DateTime.UtcNow;
        var created = await service.CreateAsync(entity, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Payment entity, CancellationToken ct)
    {
        if (entity.Id == 0) entity.Id = id;
        if (entity.Id != id) return BadRequest("Mismatched id");
        
        var existing = await service.GetAsync(id, ct);
        if (existing is null) return NotFound();
        
        entity.UpdatedAt = DateTime.UtcNow;
        await service.UpdateAsync(entity, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var existing = await service.GetAsync(id, ct);
        if (existing is null) return NotFound();
        
        await service.DeleteAsync(existing, ct);
        return NoContent();
    }
}

