using AuthService.Application.Abstractions;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Services;
using AuthService.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.EntityFrameworkCore;
using PvpAnalytics.Shared.Security;

namespace AuthService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        if ((connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase)) &&
            !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invalid connection string for AuthService: AuthService must use Oracle Database, but detected PostgreSQL connection string format. " +
                $"Connection string starts with: {connectionString[..Math.Min(50, connectionString.Length)]}... " +
                $"Check for environment variable 'ConnectionStrings__DefaultConnection' that might be overriding the Oracle connection string.");
        }

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            var databaseName = connectionString["InMemory:".Length..];
            services.AddDbContext<AuthDbContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            services.AddDbContext<AuthDbContext>(options =>
                options.UseOracle(connectionString));
        }

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<AuthService.Application.Abstractions.IProfileService, ProfileService>();

        return services;
    }
}


