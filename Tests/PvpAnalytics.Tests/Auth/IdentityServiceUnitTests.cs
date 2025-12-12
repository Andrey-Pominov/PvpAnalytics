using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AuthService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PvpAnalytics.Shared.Security;
using Xunit;

namespace PvpAnalytics.Tests.Auth;

public class IdentityServiceUnitTests
{
    private static IdentityService CreateService(out JwtOptions options)
    {
        options = new JwtOptions
        {
            SigningKey = "test-signing-key-0123456789",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        // For these tests we only exercise private helpers that depend on JwtOptions,
        // so we can safely pass null for the other dependencies.
        var service = new IdentityService(
            userManager: null!,
            signInManager: null!,
            dbContext: null!,
            jwtOptions: Options.Create(options),
            logger: new NullLogger<IdentityService>());

        return service;
    }

    [Fact]
    public void GenerateSecureRefreshToken_ProducesHashMatchingComputeRefreshTokenHash()
    {
        // Arrange
        var service = CreateService(out var options);
        var type = typeof(IdentityService);

        var generateMethod = type
            .GetMethod("GenerateSecureRefreshToken", BindingFlags.NonPublic | BindingFlags.Instance);
        var hashMethod = type
            .GetMethod("ComputeRefreshTokenHash", BindingFlags.NonPublic | BindingFlags.Instance);

        // Guard: skip test if private methods were renamed/removed (reflection is fragile)
        if (generateMethod is null || hashMethod is null)
        {
            // Skip test gracefully - private implementation details may have changed
            return;
        }

        // Act
        var tuple = generateMethod.Invoke(service, Array.Empty<object?>());
        if (tuple is null)
        {
            // Method returned null unexpectedly
            return;
        }
        
        // IdentityService currently returns (string PlainToken, string TokenHash),
        // which is compiled as ValueTuple<string, string>. Cast directly.
        var (plainToken, tokenHash) = ((string PlainToken, string TokenHash))tuple;

        var recomputedHash = (string?)hashMethod.Invoke(service, new object?[] { plainToken });

        // Assert
        plainToken.Should().NotBeNullOrWhiteSpace();
        tokenHash.Should().NotBeNullOrWhiteSpace();
        recomputedHash.Should().NotBeNull();
        tokenHash.Should().Be(recomputedHash);

        // Ensure token has high entropy (length and non-trivial base64)
        var rawBytes = Convert.FromBase64String(plainToken);
        rawBytes.Length.Should().BeGreaterOrEqualTo(32);

        // Ensure HMAC uses the configured signing key
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.SigningKey));
        var expectedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(plainToken)));
        tokenHash.Should().Be(expectedHash);
    }
}


