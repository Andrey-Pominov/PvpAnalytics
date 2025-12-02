using System.Reflection;
using FluentAssertions;
using PvpAnalytics.Application.Services;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using Xunit;

namespace PvpAnalytics.Tests.KeyMoments;

public class KeyMomentServiceTests
{
    private static MethodInfo GetPrivateStaticMethod(string name)
    {
        var method = typeof(KeyMomentService).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);

        return method ?? throw new InvalidOperationException($"Method '{name}' not found via reflection.");
    }

    [Fact]
    public void IsPlayerInactiveAfterDamage_ReturnsTrue_WhenNoFurtherEventsWithinFiveSeconds()
    {
        // Arrange
        var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        const long targetPlayerId = 2;
        const long sourcePlayerId = 1;

        var initialHit = new CombatLogEntry
        {
            Id = 1,
            MatchId = 1,
            Timestamp = now,
            SourcePlayerId = sourcePlayerId,
            TargetPlayerId = targetPlayerId,
            DamageDone = 60_000,
            HealingDone = 0,
            Ability = "Chaos Bolt",
            CrowdControl = string.Empty
        };

        // Event from the target player AFTER the 5s window -> should still be considered inactive
        var lateActivity = new CombatLogEntry
        {
            Id = 2,
            MatchId = 1,
            Timestamp = now.AddSeconds(6), // > 5s window
            SourcePlayerId = targetPlayerId,
            TargetPlayerId = 999,
            DamageDone = 1_000,
            HealingDone = 0,
            Ability = "Shadow Bolt",
            CrowdControl = string.Empty
        };

        var logs = new List<CombatLogEntry> { initialHit, lateActivity };

        var method = GetPrivateStaticMethod("IsPlayerInactiveAfterDamage");

        // Act
        var result = (bool)method.Invoke(
            null,
            new object[] { logs, initialHit, targetPlayerId })!;

        // Assert
        result.Should().BeTrue("the target player has no activity within 5 seconds after the damage event");
    }

    [Fact]
    public void IsPlayerInactiveAfterDamage_HasCorrectBoundaryBehavior_AroundFiveSeconds()
    {
        // Arrange
        var now = new DateTime(2024, 1, 1, 13, 0, 0, DateTimeKind.Utc);
        const long targetPlayerId = 10;

        var baseHit = new CombatLogEntry
        {
            Id = 1,
            MatchId = 1,
            Timestamp = now,
            SourcePlayerId = 99,
            TargetPlayerId = targetPlayerId,
            DamageDone = 60_000,
            HealingDone = 0,
            Ability = "Fireball",
            CrowdControl = string.Empty
        };

        var justBelowFiveSeconds = new CombatLogEntry
        {
            Id = 2,
            MatchId = 1,
            Timestamp = now.AddSeconds(4.999),
            SourcePlayerId = targetPlayerId,
            TargetPlayerId = 999,
            DamageDone = 500,
            HealingDone = 0,
            Ability = "Minor Hit",
            CrowdControl = string.Empty
        };

        var exactlyFiveSeconds = new CombatLogEntry
        {
            Id = 3,
            MatchId = 1,
            Timestamp = now.AddSeconds(5.0),
            SourcePlayerId = targetPlayerId,
            TargetPlayerId = 999,
            DamageDone = 500,
            HealingDone = 0,
            Ability = "Boundary Hit",
            CrowdControl = string.Empty
        };

        var justAboveFiveSeconds = new CombatLogEntry
        {
            Id = 4,
            MatchId = 1,
            Timestamp = now.AddSeconds(5.001),
            SourcePlayerId = targetPlayerId,
            TargetPlayerId = 999,
            DamageDone = 500,
            HealingDone = 0,
            Ability = "Late Hit",
            CrowdControl = string.Empty
        };

        var method = GetPrivateStaticMethod("IsPlayerInactiveAfterDamage");

        // Act & Assert
        // Activity at 4.999s should be inside the 5s window -> NOT inactive
        var resultBelow = (bool)method.Invoke(
            null,
            new object[] { new List<CombatLogEntry> { baseHit, justBelowFiveSeconds }, baseHit, targetPlayerId })!;
        resultBelow.Should().BeFalse("activity at 4.999s is within the 5-second window");

        // Activity at exactly 5.0s should be inside the window (<= 5s) -> NOT inactive
        var resultExactly = (bool)method.Invoke(
            null,
            new object[] { new List<CombatLogEntry> { baseHit, exactlyFiveSeconds }, baseHit, targetPlayerId })!;
        resultExactly.Should().BeFalse("activity at exactly 5.0s is within the 5-second window");

        // Activity at 5.001s should be outside the window -> inactive
        var resultAbove = (bool)method.Invoke(
            null,
            new object[] { new List<CombatLogEntry> { baseHit, justAboveFiveSeconds }, baseHit, targetPlayerId })!;
        resultAbove.Should().BeTrue("activity at 5.001s is outside the 5-second window");
    }

    [Fact]
    public void DetectDeaths_CreatesDeathMoments_ForHighDamageEvents()
    {
        // Arrange
        var matchStart = new DateTime(2024, 1, 1, 14, 0, 0, DateTimeKind.Utc);
        const long targetPlayerId = 20;
        const long killerPlayerId = 30;

        // Target player acts as a source earlier in the match, so they are tracked in playerLastActivity.
        var targetEarlyActivity = new CombatLogEntry
        {
            Id = 1,
            MatchId = 1,
            Timestamp = matchStart.AddSeconds(5),
            SourcePlayerId = targetPlayerId,
            TargetPlayerId = killerPlayerId,
            DamageDone = 1_000,
            HealingDone = 0,
            Ability = "Opening Hit",
            CrowdControl = string.Empty
        };

        // High-damage lethal hit on the target
        var lethalHit = new CombatLogEntry
        {
            Id = 2,
            MatchId = 1,
            Timestamp = matchStart.AddSeconds(20),
            SourcePlayerId = killerPlayerId,
            TargetPlayerId = targetPlayerId,
            DamageDone = 100_000, // Above the lethal threshold used by IsPotentialDeath
            HealingDone = 0,
            Ability = "Massive Crit",
            CrowdControl = string.Empty
        };

        // Activity from some *other* player after the lethal hit should not affect the target's inactivity.
        var unrelatedActivity = new CombatLogEntry
        {
            Id = 3,
            MatchId = 1,
            Timestamp = matchStart.AddSeconds(22),
            SourcePlayerId = 999,
            TargetPlayerId = killerPlayerId,
            DamageDone = 500,
            HealingDone = 0,
            Ability = "Unrelated",
            CrowdControl = string.Empty
        };

        var logs = new List<CombatLogEntry> { targetEarlyActivity, lethalHit, unrelatedActivity };

        var method = GetPrivateStaticMethod("DetectDeaths");

        // Act
        var result = (List<KeyMoment>)method.Invoke(
            null,
            new object[] { logs, matchStart })!;

        // Assert
        result.Should().HaveCount(1, "one lethal high-damage event should produce a single death moment");

        var death = result[0];
        death.EventType.Should().Be("death");
        death.TargetPlayerId.Should().Be(targetPlayerId);
        death.SourcePlayerId.Should().Be(killerPlayerId);
        death.DamageDone.Should().Be(lethalHit.DamageDone);

        var expectedRelativeTimestamp = (long)(lethalHit.Timestamp - matchStart).TotalSeconds;
        death.Timestamp.Should().Be(expectedRelativeTimestamp);
    }
}


