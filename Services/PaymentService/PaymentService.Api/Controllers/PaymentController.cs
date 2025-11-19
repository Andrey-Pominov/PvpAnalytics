using System.Security.Claims;
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
    private const string AdminRole = "Admin";

    private string? GetCurrentUserId()
    {
        // Try NameIdentifier first (standard claim type)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Fallback to "sub" claim (JWT standard, used by AuthService)
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = User.FindFirst("sub")?.Value;
        }
        
        return userId;
    }

    private bool IsAdmin()
    {
        return User.IsInRole(AdminRole);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetAll(CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        // Admins can see all payments, regular users only see their own
        if (IsAdmin())
        {
            return Ok(await service.GetAllAsync(ct));
        }

        var userPayments = await service.FindAsync(p => p.UserId == currentUserId, ct);
        return Ok(userPayments);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Payment>> Get(long id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        var entity = await service.GetAsync(id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        // Admins can access any payment, regular users can only access their own
        if (!IsAdmin() && entity.UserId != currentUserId)
        {
            return Forbid("You do not have permission to access this payment.");
        }

        return Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> Create([FromBody] Payment entity, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        if (entity.Id != 0)
        {
            return BadRequest("Id should not be set when creating a new payment.");
        }

        // Ensure the payment is created for the current user
        entity.UserId = currentUserId;
        entity.CreatedAt = DateTime.UtcNow;
        var created = await service.CreateAsync(entity, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Payment entity, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        if (entity.Id == 0) entity.Id = id;
        if (entity.Id != id) return BadRequest("Mismatched id");

        var existing = await service.GetAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        // Admins can update any payment, regular users can only update their own
        if (!IsAdmin() && existing.UserId != currentUserId)
        {
            return Forbid("You do not have permission to update this payment.");
        }

        // Prevent changing ownership unless admin
        if (!IsAdmin() && entity.UserId != currentUserId)
        {
            return Forbid("You cannot change the payment owner.");
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await service.UpdateAsync(entity, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        var existing = await service.GetAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        // Admins can delete any payment, regular users can only delete their own
        if (!IsAdmin() && existing.UserId != currentUserId)
        {
            return Forbid("You do not have permission to delete this payment.");
        }

        await service.DeleteAsync(existing, ct);
        return NoContent();
    }
}

