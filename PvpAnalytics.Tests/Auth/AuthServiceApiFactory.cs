using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PvpAnalytics.Shared.Security;

namespace PvpAnalytics.Tests.Auth;

public sealed class AuthServiceApiFactory : WebApplicationFactory<AuthService.Api.Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        Environment.SetEnvironmentVariable("EfMigrations__Skip", "true");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "AuthTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "AuthTests");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                [$"{JwtOptions.SectionName}:Issuer"] = "AuthTests",
                [$"{JwtOptions.SectionName}:Audience"] = "AuthTests",
                [$"{JwtOptions.SectionName}:SigningKey"] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            });
        });

        builder.ConfigureServices(services =>
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.RemoveAll(typeof(AuthDbContext));
            services.RemoveAll(typeof(DbContextOptions<AuthDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<AuthDbContext>));
            services.RemoveAll(typeof(IConfigureOptions<DbContextOptions<AuthDbContext>>));
            services.RemoveAll(typeof(IPostConfigureOptions<DbContextOptions<AuthDbContext>>));

            services.AddSingleton(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>()
                    .UseSqlite(_connection)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                return optionsBuilder.Options;
            });

            services.AddScoped<AuthDbContext>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

