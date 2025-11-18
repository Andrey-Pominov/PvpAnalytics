using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Services;
using PaymentService.Core.Entities;

namespace PaymentService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICrudService<Payment>, PaymentService>();
        
        return services;
    }
}

