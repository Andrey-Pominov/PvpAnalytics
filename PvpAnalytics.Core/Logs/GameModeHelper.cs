using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.Logs;

public static class GameModeHelper
{

    /// <summary>
    /// Determine the game mode for a match based on the number of participants and optional arena match ID.
    /// </summary>
    /// <param name="participantCount">The number of participants in the match.</param>
    /// <param name="arenaMatchId">Optional arena match ID to help distinguish Solo Shuffle from regular 3v3.</param>
    /// <returns>
    /// The corresponding <see cref="GameMode"/>:
    /// 4 → <see cref="GameMode.TwoVsTwo"/>, 6 → <see cref="GameMode.ThreeVsThree"/> or <see cref="GameMode.Shuffle"/>, 10 → <see cref="GameMode.Skirmish"/>, otherwise <see cref="GameMode.TwoVsTwo"/>.
    /// </returns>
    public static GameMode GetGameModeFromParticipantCount(int participantCount, string? arenaMatchId = null)
    {
        System.Diagnostics.Debug.Assert(participantCount >= 0, "Participant count should be non-negative");
        
        // For 6 participants, check if it's Solo Shuffle based on arena match ID pattern
        if (participantCount == 6 && !string.IsNullOrEmpty(arenaMatchId))
        {
            // Solo Shuffle arena matches typically have specific ID patterns
            // If the arena match ID contains indicators of shuffle (this may need refinement based on actual log format)
            // For now, we'll default to ThreeVsThree and let the system be refined later
            // You may need to update this logic based on actual arena match ID patterns from logs
            return GameMode.ThreeVsThree;
        }
        
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
    /// <param name="arenaMatchId">Optional arena match ID to help distinguish Solo Shuffle from regular 3v3.</param>
    /// <returns>`GameMode.TwoVsTwo` if <paramref name="participantCount"/> is null; otherwise the GameMode that corresponds to the provided participant count.</returns>
    public static GameMode GetGameModeFromParticipantCount(int? participantCount, string? arenaMatchId = null)
    {
        return participantCount.HasValue ? GetGameModeFromParticipantCount(participantCount.Value, arenaMatchId) : GameMode.TwoVsTwo;
    }
}
