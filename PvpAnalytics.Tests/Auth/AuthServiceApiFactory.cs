using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PvpAnalytics.Shared.Security;

namespace PvpAnalytics.Tests.Auth;

public sealed class AuthServiceApiFactory : WebApplicationFactory<AuthService.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        Environment.SetEnvironmentVariable("EfMigrations__Skip", "true");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "AuthTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "AuthTests");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

        var inMemoryConnection = $"InMemory:AuthServiceTests_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", inMemoryConnection);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = inMemoryConnection,
                [$"{JwtOptions.SectionName}:Issuer"] = "AuthTests",
                [$"{JwtOptions.SectionName}:Audience"] = "AuthTests",
                [$"{JwtOptions.SectionName}:SigningKey"] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            });
        });
    }
}

