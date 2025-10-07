using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PvpAnalytics.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PvpAnalyticsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));
        
        return services;
    }
}