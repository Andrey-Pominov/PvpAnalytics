using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace PvpAnalytics.Tests.Payment;

public sealed class PaymentServiceApiFactory : WebApplicationFactory<PaymentService.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly string? _connectionString;

    public PaymentServiceApiFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("payment_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
        
        // Start container synchronously for connection string availability
        // This is acceptable for test setup
        _postgresContainer.StartAsync().GetAwaiter().GetResult();
        _connectionString = _postgresContainer.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Connection string will be set after container starts
            // Use placeholder for now, will be updated in InitializeAsync
            var connectionString = _connectionString ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=payment_test";
            
            // Add test configuration - this will be added last and override app settings
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EfMigrations:Skip"] = "true",
                ["UseInMemoryDatabase"] = "false", // Use TestContainers Postgres SQL instead
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:SigningKey"] = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration from Infrastructure layer
            var dbContextDescriptors = services.Where(d => 
                d.ServiceType == typeof(PaymentDbContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) && d.ServiceType.GetGenericArguments()[0] == typeof(PaymentDbContext)) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptionsBuilder<>) && d.ServiceType.GetGenericArguments()[0] == typeof(PaymentDbContext)))
                .ToList();
            
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Register DbContext with TestContainer connection string
            //  is available after container starts in constructor
            services.AddDbContext<PaymentDbContext>(options =>
            {
                var connectionString = _connectionString ?? _postgresContainer.GetConnectionString();
                options.UseNpgsql(connectionString);
            });

            // Override authentication to use test handler
            // PostConfigure runs after Program.cs, allowing us to change defaults
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestPaymentAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestPaymentAuthHandler.AuthenticationScheme;
                options.DefaultForbidScheme = TestPaymentAuthHandler.AuthenticationScheme;
            });

            // Register test authentication handler
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestPaymentAuthHandler>(
                    TestPaymentAuthHandler.AuthenticationScheme, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        // Container is already started in constructor
        // Run migrations on the TestContainer database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task CleanupDatabaseAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            
            // Ensure database schema exists
            await dbContext.Database.EnsureCreatedAsync();
            
            // Remove all payments (truncate is faster but RemoveRange is safer)
            var payments = await dbContext.Payments.ToListAsync();
            if (payments.Count > 0)
            {
                dbContext.Payments.RemoveRange(payments);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex) when (ex.Message.Contains("database providers") || 
                                   ex.Message.Contains("does not exist") ||
                                   ex.Message.Contains("relation") ||
                                   ex.Message.Contains("table"))
        {
            // If we get a provider conflict or database/table doesn't exist, ensure it's created
            try
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }
            catch
            {
                // Ignore errors during recovery attempt
            }
        }
    }
}

