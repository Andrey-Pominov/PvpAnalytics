using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "AuthService.Api");
        if (!Directory.Exists(apiProjectPath))
        {
            apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "AuthService.Api");
            if (!Directory.Exists(apiProjectPath))
            {
                apiProjectPath = Directory.GetCurrentDirectory();
            }
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("AUTHSERVICE_DESIGNTIME_CONNECTION");
        }
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found or empty. " +
                "Please configure it in appsettings.json, appsettings.Development.json, " +
                "or set AUTHSERVICE_DESIGNTIME_CONNECTION environment variable. ");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseOracle(connectionString);

        return new AuthDbContext(optionsBuilder.Options);
    }
}

