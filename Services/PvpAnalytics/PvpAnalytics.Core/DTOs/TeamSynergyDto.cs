namespace PvpAnalytics.Core.DTOs;

public class TeamSynergyDto
{
    public long TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public List<PartnerSynergyDto> PartnerSynergies { get; set; } = new();
    public Dictionary<string, double> MapWinRates { get; set; } = new();
    public Dictionary<string, double> CompositionWinRates { get; set; } = new();
    public double OverallSynergyScore { get; set; }
}

public class PartnerSynergyDto
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
}

