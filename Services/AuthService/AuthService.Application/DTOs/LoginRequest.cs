using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password);


