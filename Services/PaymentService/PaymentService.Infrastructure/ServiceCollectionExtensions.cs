using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Core.Repositories;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            options.ConfigureWarnings(warnings =>
                warnings.Log(RelationalEventId.PendingModelChangesWarning));

        });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}