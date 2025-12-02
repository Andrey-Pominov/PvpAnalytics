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
        if (participantCount != 6 || string.IsNullOrEmpty(arenaMatchId))
            return participantCount switch
            {
                4 => GameMode.TwoVsTwo,
                6 => GameMode.ThreeVsThree,
                10 => GameMode.Skirmish,
                _ => GameMode.TwoVsTwo
            };
        // Solo Shuffle arena matches typically have "shuffle" in the arena match ID
        if (arenaMatchId.Contains("shuffle", StringComparison.OrdinalIgnoreCase))
        {
            return GameMode.Shuffle;
        }
        // If no shuffle indicator found, fall through to default ThreeVsThree logic

        return participantCount switch
        {
            6 => GameMode.ThreeVsThree,
            _ => throw new ArgumentOutOfRangeException(nameof(participantCount), participantCount, null)
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
