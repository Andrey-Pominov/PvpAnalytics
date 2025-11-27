using LoggingService.Application.Abstractions;
using LoggingService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoggingService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing or invalid. " +
                "Please configure it in appsettings.json, environment variables, or user secrets.");
        }

        services.AddDbContext<LoggingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILoggingService, Services.LoggingService>();
        services.AddScoped<IServiceRegistry, Services.ServiceRegistry>();

        return services;
    }
}

