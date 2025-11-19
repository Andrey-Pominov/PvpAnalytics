using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Models;
using PaymentService.Application.Services;
using PaymentService.Core.Entities;
using PaymentService.Core.Enum;
using PaymentService.Core.Repositories;

namespace PaymentService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentController(ICrudService<Payment> service, IRepository<Payment> repository) : ControllerBase
{
    private const string AdminRole = "Admin";
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

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
    public async Task<ActionResult<PaginatedResponse<Payment>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? userId = null,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken ct = default)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        // Start with queryable
        var query = repository.GetQueryable();

        // Apply user scope filter (non-admins only see their own payments)
        if (!IsAdmin())
        {
            query = query.Where(p => p.UserId == currentUserId);
        }
        else if (!string.IsNullOrWhiteSpace(userId))
        {
            // Admins can filter by userId if provided
            query = query.Where(p => p.UserId == userId);
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        // Apply date range filters
        if (startDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= endDate.Value);
        }

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "id" => sortOrder.ToLower() == "asc" ? query.OrderBy(p => p.Id) : query.OrderByDescending(p => p.Id),
            "amount" => sortOrder.ToLower() == "asc" ? query.OrderBy(p => p.Amount) : query.OrderByDescending(p => p.Amount),
            "status" => sortOrder.ToLower() == "asc" ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
            "userid" => sortOrder.ToLower() == "asc" ? query.OrderBy(p => p.UserId) : query.OrderByDescending(p => p.UserId),
            "createdat" => sortOrder.ToLower() == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
            "updatedat" => sortOrder.ToLower() == "asc" ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // Default to createdAt desc
        };

        // Get paginated results
        var (items, total) = await repository.GetPagedAsync(query, page, pageSize, ct);

        var response = new PaginatedResponse<Payment>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
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
    public async Task<ActionResult<Payment>> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        // Create payment entity from DTO
        var payment = new Payment
        {
            Amount = request.Amount,
            TransactionId = request.TransactionId,
            PaymentMethod = request.PaymentMethod,
            Description = request.Description,
            Status = PaymentStatus.Pending, // Default to Pending
            UserId = currentUserId, // Set from JWT token
            CreatedAt = DateTime.UtcNow // Set server timestamp
        };

        var created = await service.CreateAsync(payment, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePaymentRequest request, CancellationToken ct)
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

        // Admins can update any payment, regular users can only update their own
        if (!IsAdmin() && existing.UserId != currentUserId)
        {
            return Forbid("You do not have permission to update this payment.");
        }

        // Update only writable fields (selective update to prevent modifying immutable fields)
        existing.Amount = request.Amount;
        existing.Status = request.Status;
        existing.Description = request.Description;
        existing.UpdatedAt = DateTime.UtcNow; // Set server timestamp

        await service.UpdateAsync(existing, ct);
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

