using System.Reflection;
using FluentAssertions;
using PvpAnalytics.Application.Logs;
using Xunit;

namespace PvpAnalytics.Tests.Logs;

public class LuaTableParserTests
{
    [Fact]
    public void TryStartNewMatch_AllowsCommentedListEntries()
    {
        // Arrange
        var lines = new[]
        {
            "PvPAnalyticsDB = {", 
            "{",
            "[\"Logs\"] = {",
            "23:07:55 - HEAL: Cyclonex-Hellfire healed with Thriving Vegetation for 556252,",
            "23:07:56 - HEAL: Cyclonex-Hellfire healed with Symbiotic Relationship for 62757,",
            "}}}"
        };

        var luaType = typeof(LuaTableParser);
        var stateType = luaType.GetNestedType("ManualParserState", BindingFlags.NonPublic)!;
        var state = Activator.CreateInstance(stateType)!;

        var method = luaType.GetMethod(
            "TryStartNewMatch",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        // Act
        var result = (bool)method.Invoke(null, [lines, 2, "{", state])!;

        // Assert
        result.Should().BeTrue();
    }
}

