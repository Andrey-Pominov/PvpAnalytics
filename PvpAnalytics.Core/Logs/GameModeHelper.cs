using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.Logs;

public static class GameModeHelper
{
    /// <summary>
    /// Maps participant count to GameMode enum.
    /// 4 participants -> TwoVsTwo
    /// 6 participants -> ThreeVsThree
    /// 10 participants -> Skirmish (no FiveVsFive in enum)
    /// Otherwise -> TwoVsTwo (default)
    /// </summary>
    public static GameMode GetGameModeFromParticipantCount(int participantCount)
    {
        // Assertions for expected mappings
        System.Diagnostics.Debug.Assert(participantCount >= 0, "Participant count should be non-negative");
        
        return participantCount switch
        {
            4 => GameMode.TwoVsTwo,      // 2v2 = 4 total players
            6 => GameMode.ThreeVsThree,  // 3v3 = 6 total players
            10 => GameMode.Skirmish,     // Note: No FiveVsFive in enum, using Skirmish for 10 players
            _ => GameMode.TwoVsTwo       // Default fallback for edge cases (0, 1, 2, 3, 5, 7, 8, 9, 11+)
        };
    }

    /// <summary>
    /// Maps participant count to GameMode enum, with null handling.
    /// Returns TwoVsTwo for null or invalid counts.
    /// </summary>
    public static GameMode GetGameModeFromParticipantCount(int? participantCount)
    {
        return participantCount.HasValue ? GetGameModeFromParticipantCount(participantCount.Value) : GameMode.TwoVsTwo;
    }
}

