using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Abstractions;
using AuthService.Application.DTOs;
using AuthService.Application.Models;
using AuthService.Core.Entities;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PvpAnalytics.Shared.Security;

namespace AuthService.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AuthDbContext dbContext,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(";", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return await GenerateTokensForUserAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        return await GenerateTokensForUserAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var tokenHash = ComputeRefreshTokenHash(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new InvalidOperationException("Invalid refresh token.");
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        await RevokeRefreshTokenAsync(refreshToken.Id, ct);

        return await GenerateTokensForUserAsync(user, ct);
    }

    private async Task<AuthResponse> GenerateTokensForUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.AccessTokenMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim("name", user.FullName));
        }

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        var (refreshTokenPlain, refreshTokenHash) = GenerateSecureRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            expires,
            refreshTokenPlain,
            refreshToken.ExpiresAt);
    }

    private async Task RevokeRefreshTokenAsync(Guid refreshTokenId, CancellationToken ct)
    {
        var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == refreshTokenId, ct);
        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(ct);
    }

    private (string PlainToken, string TokenHash) GenerateSecureRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        var plainToken = Convert.ToBase64String(bytes);
        var tokenHash = ComputeRefreshTokenHash(plainToken);
        return (plainToken, tokenHash);
    }

    private string ComputeRefreshTokenHash(string token)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}


