using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PvpAnalytics.Tests.Payment;

public sealed class TestPaymentAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "TestPayment";
    public const string TestUserIdHeader = "X-Test-User-Id";
    public const string TestRolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the test user ID header is present
        if (!Request.Headers.TryGetValue(TestUserIdHeader, out var userIdHeader) || 
            string.IsNullOrWhiteSpace(userIdHeader))
        {
            // No test user header means unauthenticated request
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdHeader.ToString();
        var identity = new ClaimsIdentity(AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new Claim("sub", userId));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Test User"));

        // Check for roles header
        if (Request.Headers.TryGetValue(TestRolesHeader, out var rolesHeader) && 
            !string.IsNullOrWhiteSpace(rolesHeader))
        {
            var roles = rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role.Trim()));
            }
        }

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Extension method for creating authenticated clients
public static class PaymentServiceApiFactoryExtensions
{
    public static HttpClient CreateAuthenticatedClient(
        this PaymentServiceApiFactory factory,
        string userId = "test-user-123",
        string[]? roles = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestPaymentAuthHandler.TestUserIdHeader, userId);
        if (roles is { Length: > 0 })
        {
            client.DefaultRequestHeaders.Add(TestPaymentAuthHandler.TestRolesHeader, string.Join(",", roles));
        }
        return client;
    }
}

