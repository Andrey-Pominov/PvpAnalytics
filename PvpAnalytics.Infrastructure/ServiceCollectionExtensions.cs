using Microsoft.EntityFrameworkCore;
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
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}