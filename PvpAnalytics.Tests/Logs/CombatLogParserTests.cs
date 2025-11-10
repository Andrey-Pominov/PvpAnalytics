using FluentAssertions;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Logs;
using Xunit;

namespace PvpAnalytics.Tests.Logs;

public class CombatLogParserTests
{
    [Fact]
    public void ParseLine_ReturnsNull_WhenLineIsEmpty()
    {
        CombatLogParser.ParseLine(string.Empty).Should().BeNull();
    }

    [Fact]
    public void ParseLine_ParsesZoneChangeEvent()
    {
        const string line = "1/2/2024 19:10:03.123  ZONE_CHANGE,559,Nagrand Arena,,,,,,,,,,,";

        var result = CombatLogParser.ParseLine(line);

        result.Should().NotBeNull();
        result!.EventType.Should().Be(CombatLogEventTypes.ZoneChange);
        result.ZoneId.Should().Be(559);
        result.ZoneName.Should().Be("Nagrand Arena");
    }

    [Fact]
    public void ParseLine_ParsesSpellDamageEvent()
    {
        const string line =
            "1/2/2024 19:10:05.456  SPELL_DAMAGE,0x0100000000000001,Player-One,0x0,0x0,0x0200000000000002,Player-Two,0x0,0x0,1337,Fireball,0x0,123,0,0,0,0,0,0,0";

        var result = CombatLogParser.ParseLine(line);

        result.Should().NotBeNull();
        result!.SourceName.Should().Be("Player-One");
        result.TargetName.Should().Be("Player-Two");
        result.SpellName.Should().Be("Fireball");
        result.Damage.Should().Be(123);
    }

    [Fact]
    public void IsArenaZone_ReturnsTrue_ForKnownArena()
    {
        CombatLogParser.IsArenaZone(559).Should().BeTrue();
    }
}

