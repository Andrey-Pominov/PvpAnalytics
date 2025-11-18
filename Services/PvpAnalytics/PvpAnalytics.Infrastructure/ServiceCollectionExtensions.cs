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
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
            // Log the pending model changes warning instead of throwing
            options.ConfigureWarnings(warnings =>
                warnings.Log(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}