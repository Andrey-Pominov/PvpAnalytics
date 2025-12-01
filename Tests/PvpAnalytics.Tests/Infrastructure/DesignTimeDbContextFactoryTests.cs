using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Infrastructure;
using Xunit;

namespace PvpAnalytics.Tests.Infrastructure;

public class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_UsesEnvironmentFallback_WhenConnectionStringMissing()
    {
        // Arrange
        const string fallback = "Host=localhost;Port=5432;Database=PvpAnalytics_DesignTime;Username=postgres";
        var original = Environment.GetEnvironmentVariable("PVPANALYTICS_DESIGNTIME_CONNECTION");
        Environment.SetEnvironmentVariable("PVPANALYTICS_DESIGNTIME_CONNECTION", fallback);

        try
        {
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);

            // Assert
            context.Should().NotBeNull();
            context.Database.ProviderName.Should().Contain("Npgsql");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PVPANALYTICS_DESIGNTIME_CONNECTION", original);
        }
    }
}


