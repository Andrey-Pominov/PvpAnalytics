using FluentAssertions;
using PvpAnalytics.Application.Logs;
using Xunit;

namespace PvpAnalytics.Tests.Logs;

public class LuaTableParserTests
{
    [Theory]
    [InlineData(@"HH:mm:ss - EVENT: simple", "HH:mm:ss - EVENT: simple")]
    [InlineData(@"HH:mm:ss - EVENT: with \""quote\""", "HH:mm:ss - EVENT: with \"quote\"")]
    [InlineData(@"HH:mm:ss - EVENT: path C:\\folder\\file", @"HH:mm:ss - EVENT: path C:\folder\file")]
    [InlineData(@"HH:mm:ss - EVENT: combo C:\\\""path\""", @"HH:mm:ss - EVENT: combo C:\"path\"")]
    public void UnescapeLuaString_HandlesEscapedQuotesAndBackslashes(string input, string expected)
    {
        // Arrange / Act
        var result = typeof(LuaTableParser)
            .GetMethod("UnescapeLuaString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { input }) as string;

        // Assert
        result.Should().Be(expected);
    }
}


