using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.Logs;

public static class GameModeHelper
{

    /// <summary>
    /// Determine the game mode for a match based on the number of participants.
    /// </summary>
    /// <param name="participantCount">The number of participants in the match.</param>
    /// <returns>
    /// The corresponding <see cref="GameMode"/>:
    /// 4 → <see cref="GameMode.TwoVsTwo"/>, 6 → <see cref="GameMode.ThreeVsThree"/>, 10 → <see cref="GameMode.Skirmish"/>, otherwise <see cref="GameMode.TwoVsTwo"/>.
    /// </returns>
    public static GameMode GetGameModeFromParticipantCount(int participantCount)
    {
        System.Diagnostics.Debug.Assert(participantCount >= 0, "Participant count should be non-negative");
        
        return participantCount switch
        {
            4 => GameMode.TwoVsTwo,     
            6 => GameMode.ThreeVsThree, 
            10 => GameMode.Skirmish,
            _ => GameMode.TwoVsTwo
        };
    }
    
    /// <summary>
    /// Determines the GameMode corresponding to an optional participant count.
    /// </summary>
    /// <param name="participantCount">Number of participants in the match; when null, the participant count is unknown.</param>
    /// <returns>`GameMode.TwoVsTwo` if <paramref name="participantCount"/> is null; otherwise the GameMode that corresponds to the provided participant count.</returns>
    public static GameMode GetGameModeFromParticipantCount(int? participantCount)
    {
        return participantCount.HasValue ? GetGameModeFromParticipantCount(participantCount.Value) : GameMode.TwoVsTwo;
    }
}
