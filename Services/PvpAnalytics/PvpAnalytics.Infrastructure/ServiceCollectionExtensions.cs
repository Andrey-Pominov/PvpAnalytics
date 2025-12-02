using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Infrastructure.Repositories;

namespace PvpAnalytics.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PvpAnalyticsDbContext>(options =>
        {
            var provider = config["EfProvider"];
            var connectionString = config.GetConnectionString("DefaultConnection");

            if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("PvpAnalyticsDb");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Database connection string 'DefaultConnection' is not configured. " +
                        "Please provide it via appsettings.json, environment variables, or user secrets.");
                }

                options.UseNpgsql(connectionString);
            }

            // Log the pending model changes warning instead of throwing
            options.ConfigureWarnings(warnings =>
                warnings.Log(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}