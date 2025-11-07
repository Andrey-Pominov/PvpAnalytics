using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.Logs;

public static class GameModeHelper
{

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
    
    public static GameMode GetGameModeFromParticipantCount(int? participantCount)
    {
        return participantCount.HasValue ? GetGameModeFromParticipantCount(participantCount.Value) : GameMode.TwoVsTwo;
    }
}

