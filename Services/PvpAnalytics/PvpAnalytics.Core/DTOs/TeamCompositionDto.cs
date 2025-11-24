namespace PvpAnalytics.Core.DTOs;

public class TeamCompositionDto
{
    public long TeamId { get; set; }
    public string Composition { get; set; } = string.Empty; // e.g., "Rogue-Mage-Priest"
    public List<TeamMember> Members { get; set; } = new();
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public double AverageRating { get; set; }
    public int PeakRating { get; set; }
    public double SynergyScore { get; set; } // Calculated based on performance vs expected
    public DateTime FirstMatchDate { get; set; }
    public DateTime LastMatchDate { get; set; }
}

public class TeamMember
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? Spec { get; set; }
}

public class PlayerSynergyDto
{
    public long Player1Id { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public long Player2Id { get; set; }
    public string Player2Name { get; set; } = string.Empty;
    public int MatchesTogether { get; set; }
    public int WinsTogether { get; set; }
    public double WinRateTogether { get; set; }
    public double AverageRatingTogether { get; set; }
    public double SynergyScore { get; set; }
    public List<TeamCompositionDto> CommonCompositions { get; set; } = new();
}

public class PlayerTeamsDto
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public List<TeamCompositionDto> Teams { get; set; } = new();
}

