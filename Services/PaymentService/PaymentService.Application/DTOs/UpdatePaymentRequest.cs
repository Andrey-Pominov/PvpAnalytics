using System.ComponentModel.DataAnnotations;
using PaymentService.Core.Enum;

namespace PaymentService.Application.DTOs;

public class UpdatePaymentRequest
{
    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "Amount must be between 0.01 and 999,999,999.99")]
    public decimal Amount { get; set; }
    
    [Required]
    public PaymentStatus Status { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
}

