using FluentAssertions;
using PvpAnalytics.Application.Logs;
using PvpAnalytics.Core.Logs;
using Xunit;

namespace PvpAnalytics.Tests.Logs;

public class SimplifiedLogParserTests
{
    [Fact]
    public void ParseLine_ParsesHealEvent()
    {
        const string line = "12:34:56 - HEAL: Alice healed with Flash Heal for 1500";
        var baseDate = new DateTime(2025, 1, 2);

        var result = SimplifiedLogParser.ParseLine(line, baseDate);

        result.Should().NotBeNull();
        result!.EventType.Should().Be(CombatLogEventTypes.SpellHeal);
        result.SourceName.Should().Be("Alice");
        result.SpellName.Should().Be("Flash Heal");
        result.Healing.Should().Be(1500);
        result.Timestamp.Should().Be(baseDate.Date.Add(new TimeSpan(12, 34, 56)));
    }

    [Fact]
    public void ParseLine_ParsesDamageEvent()
    {
        const string line = "08:00:01 - DAMAGE: Bob used Shadow Bolt for 900 on Charlie";
        var baseDate = new DateTime(2025, 1, 2);

        var result = SimplifiedLogParser.ParseLine(line, baseDate);

        result.Should().NotBeNull();
        result!.EventType.Should().Be(CombatLogEventTypes.SpellDamage);
        result.SourceName.Should().Be("Bob");
        result.SpellName.Should().Be("Shadow Bolt");
        result.Damage.Should().Be(900);
        result.Timestamp.Should().Be(baseDate.Date.Add(new TimeSpan(8, 0, 1)));
    }
}


