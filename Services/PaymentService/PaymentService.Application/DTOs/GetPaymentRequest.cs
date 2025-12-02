using PaymentService.Core.Enum;

namespace PaymentService.Application.DTOs;

public class GetPaymentRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; } = 20;
     public string? UserId { get; set; } = null;
     public PaymentStatus? Status { get; set; } = null;
     public DateTime? EndDate { get; set; } = null;
     public DateTime? StartDate { get; set; } = null;
     public string SortBy { get; set; } = "createdAt";
     public string SortOrder { get; set; } = "desc";
}