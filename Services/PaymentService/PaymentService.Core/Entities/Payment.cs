using System.ComponentModel.DataAnnotations;
using PaymentService.Core.Enum;

namespace PaymentService.Core.Entities;

public class Payment
{
    public long Id { get; set; }
    
    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "Amount must be between 0.01 and 999,999,999.99")]
    public decimal Amount { get; set; }
    
    [Required]
    public PaymentStatus Status { get; set; }
    
    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
}

