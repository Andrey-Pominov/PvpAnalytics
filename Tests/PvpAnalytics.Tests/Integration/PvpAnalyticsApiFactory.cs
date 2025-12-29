using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Core.Configuration;
using PvpAnalytics.Shared.Security;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Tests.Integration;

public sealed class PvpAnalyticsApiFactory : WebApplicationFactory<Api.IProgram>
{
    public TestIngestionState IngestionState { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EfMigrations:Skip"] = "true",
                ["EfProvider"] = "InMemory",
                [$"{JwtOptions.SectionName}:Issuer"] = "TestIssuer",
                [$"{JwtOptions.SectionName}:Audience"] = "TestAudience",
                [$"{JwtOptions.SectionName}:SigningKey"] = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
                // Provide dummy but valid WowApi credentials so option validation passes in tests
                [$"{WowApiOptions.SectionName}:ClientId"] = "TestClientId",
                [$"{WowApiOptions.SectionName}:ClientSecret"] = "TestClientSecret"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(IngestionState);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });

            // Register test service as keyed services for both formats
            services.AddKeyedScoped<ICombatLogIngestionService, TestCombatLogIngestionService>(CombatLogFormat.Traditional);
            services.AddKeyedScoped<ICombatLogIngestionService, TestCombatLogIngestionService>(CombatLogFormat.LuaTable);
            
            // Register test factory
            services.AddScoped<ICombatLogIngestionServiceFactory, TestCombatLogIngestionServiceFactory>();
        });
    }
}

internal sealed class TestCombatLogIngestionService(TestIngestionState state) 
    : ICombatLogIngestionService
{
    public Task<List<Match>> IngestAsync(Stream fileStream, CancellationToken ct = default)
        => state.Handler(fileStream, ct);
}

internal sealed class TestCombatLogIngestionServiceFactory(IServiceProvider serviceProvider) 
    : ICombatLogIngestionServiceFactory
{
    public ICombatLogIngestionService GetService(CombatLogFormat format)
    {
        return serviceProvider.GetRequiredKeyedService<ICombatLogIngestionService>(format);
    }
}

public sealed class TestIngestionState
{
    private static readonly Func<Stream, CancellationToken, Task<List<Match>>> DefaultHandler = (_, _) => Task.FromResult(new List<Match>
    {
        new Match
        {
            Id = 100,
            ArenaZone = ArenaZone.DalaranArena,
            MapName = nameof(ArenaZone.DalaranArena),
            ArenaMatchId = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow,
            UniqueHash = Guid.NewGuid().ToString("N"),
            GameMode = GameMode.TwoVsTwo,
            Duration = 120,
            IsRanked = true
        }
    });

    private Func<Stream, CancellationToken, Task<List<Match>>> _handler = DefaultHandler;

    public Func<Stream, CancellationToken, Task<List<Match>>> Handler
    {
        get => _handler;
        set => _handler = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void Reset() => _handler = DefaultHandler;
}

