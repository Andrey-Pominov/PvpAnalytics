using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PvpAnalytics.Tests.Integration;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    public const string AuthenticationScheme = "Test";

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "integration-user"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Integration User"));

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

