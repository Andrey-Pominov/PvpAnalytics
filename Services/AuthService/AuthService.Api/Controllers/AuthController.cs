using AuthService.Application.Abstractions;
using AuthService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IIdentityService identityService, ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        logger.LogInformation("Register request received for email {Email}.", request.Email);
        try
        {
            var response = await identityService.RegisterAsync(request, ct);
            logger.LogInformation("Register succeeded for email {Email}.", request.Email);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Register failed for email {Email}.", request.Email);
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        logger.LogInformation("Login attempt for email {Email}.", request.Email);
        try
        {
            var response = await identityService.LoginAsync(request, ct);
            logger.LogInformation("Login succeeded for email {Email}.", request.Email);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Login failed for email {Email}.", request.Email);
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        logger.LogInformation("Refresh token requested.");
        try
        {
            var response = await identityService.RefreshTokenAsync(request, ct);
            logger.LogInformation("Refresh token succeeded.");
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Refresh token failed.");
            return BadRequest(new { error = ex.Message });
        }
    }
}


