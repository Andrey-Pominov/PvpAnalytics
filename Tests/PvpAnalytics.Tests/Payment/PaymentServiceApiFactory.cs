using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace PvpAnalytics.Tests.Payment;

public sealed class PaymentServiceApiFactory : WebApplicationFactory<PaymentService.Api.IProgram>, IAsyncLifetime
{
    private readonly PostgreSqlContainer? _postgresContainer;
    private readonly string? _connectionString;

    public PaymentServiceApiFactory()
    {
        // If Docker / Testcontainers are not available, fall back to in-memory database.
        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("payment_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithCleanUp(true)
                .Build();
            
            // Start container synchronously for connection string availability
            _postgresContainer.StartAsync().GetAwaiter().GetResult();
            _connectionString = _postgresContainer.GetConnectionString();
        }
        catch(Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            Console.WriteLine($"[PaymentServiceApiFactory] Testcontainers unavailable, using in-memory DB: {ex.Message}");
            _postgresContainer = null;
            _connectionString = null;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use a dedicated environment name so the API can relax certain validations for tests only.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // If Testcontainers failed, we'll use an in-memory database instead.
            var useInMemory = _postgresContainer is null;
            var connectionString = _connectionString ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=payment_test";
            
            // Add test configuration - this will be added last and override app settings
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EfMigrations:Skip"] = "true",
                ["UseInMemoryDatabase"] = useInMemory ? "true" : "false",
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
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) && d.ServiceType.GetGenericArguments()[0] == typeof(PaymentDbContext)))
                .ToList();
            
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Register DbContext:
            // - Use Testcontainers Postgres when available
            // - Fall back to in-memory database when Docker is not available
            services.AddDbContext<PaymentDbContext>(options =>
            {
                if (_postgresContainer is not null && _connectionString is not null)
                {
                    options.UseNpgsql(_connectionString);
                }
                else
                {
                    options.UseInMemoryDatabase("PaymentServiceTestDb");
                }
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
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        if (_postgresContainer is not null)
        {
            // Real Postgres via Testcontainers: apply migrations
            await dbContext.Database.MigrateAsync();
        }
        else
        {
            // In-memory provider: ensure schema exists
            await dbContext.Database.EnsureCreatedAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }

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

