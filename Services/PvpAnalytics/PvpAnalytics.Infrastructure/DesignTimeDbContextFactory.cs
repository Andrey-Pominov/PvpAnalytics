using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PvpAnalytics.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PvpAnalyticsDbContext>
{
    public PvpAnalyticsDbContext CreateDbContext(string[] args)
    {
        // Look for appsettings in the Api project directory
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "PvpAnalytics.Api");
        if (!Directory.Exists(apiProjectPath))
        {
            apiProjectPath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // Use a dummy connection string if not found, as it's only for design-time context creation
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=PvpAnalytics_DesignTime;Username=postgres;Password=postgres";
        }

        var optionsBuilder = new DbContextOptionsBuilder<PvpAnalyticsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PvpAnalyticsDbContext(optionsBuilder.Options);
    }
}

