using System.Reflection;
using FluentAssertions;
using PvpAnalytics.Application.Services;
using Xunit;

namespace PvpAnalytics.Tests.Analytics;

public class OpponentScoutingServiceTests
{
    [Theory]
    [InlineData(2000, 100, "Aggressive")]
    [InlineData(100, 2000, "Defensive")]
    [InlineData(500, 400, "Balanced")]
    public void DeterminePlaystyle_ComputesExpectedStyle(double avgDamage, double avgHealing, string expected)
    {
        // Arrange
        var method = typeof(OpponentScoutingService)
            .GetMethod("DeterminePlaystyle", BindingFlags.NonPublic | BindingFlags.Static)!;

        // Act
        var style = (string)method.Invoke(null, new object[] { avgDamage, avgHealing })!;

        // Assert
        style.Should().Be(expected);
    }
}


