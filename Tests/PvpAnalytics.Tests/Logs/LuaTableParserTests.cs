using System.Reflection;
using System.Text;
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

    [Fact]
    public void Parse_NewFormat_ParsesMetadataEventsAndFaction()
    {
        // Arrange
        const string content = """
                               PvPAnalyticsDB = {
                               ["players"] = {
                               ["Player-1"] = { ["faction"] = "Alliance", },
                               },
                               ["matches"] = {
                               {
                               ["players"] = {
                               ["Player-1"] = { ["name"] = "Tester" },
                               },
                               ["events"] = {
                               {
                               ["type"] = "BIG_BUTTON",
                               ["time"] = 1700000000,
                               ["spellName"] = "Test Spell",
                               ["source"] = "Alice",
                               ["dest"] = "Bob",
                               },
                               },
                               ["metadata"] = {
                               ["map"] = "Test Map",
                               ["duration"] = 10,
                               ["mode"] = "2v2",
                               ["endTime"] = "2025-01-01 00:01:00",
                               ["date"] = "2025-01-01 00:00:00",
                               },
                               },
                               },
                               }
                               """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var matches = LuaTableParser.Parse(stream);

        // Assert
        matches.Should().HaveCount(1);
        var match = matches[0];
        match.Mode.Should().Be("2v2");
        match.Zone.Should().Be("Test Map");
        match.StartTime.Should().Be("2025-01-01 00:00:00");
        match.EndTime.Should().Be("2025-01-01 00:01:00");
        match.Faction.Should().Be("Alliance");
        match.Logs.Should().ContainSingle(log =>
            log.Contains("BIG_BUTTON", StringComparison.OrdinalIgnoreCase) &&
            log.Contains("Test Spell", StringComparison.OrdinalIgnoreCase) &&
            log.Contains("Alice", StringComparison.OrdinalIgnoreCase) &&
            log.Contains("Bob", StringComparison.OrdinalIgnoreCase));
    }
}

