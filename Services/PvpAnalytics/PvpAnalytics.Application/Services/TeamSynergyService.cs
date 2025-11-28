using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface ITeamSynergyService
{
    Task<TeamSynergyDto?> GetTeamSynergyAsync(long teamId, CancellationToken ct = default);
    Task<PartnerSynergyDto?> GetPartnerSynergyAsync(long player1Id, long player2Id, CancellationToken ct = default);
}

public class TeamSynergyService(PvpAnalyticsDbContext dbContext) : ITeamSynergyService
{
    public async Task<TeamSynergyDto?> GetTeamSynergyAsync(long teamId, CancellationToken ct = default)
    {
        var team = await LoadTeamAsync(teamId, ct);
        if (team == null || team.Members.Count < 2)
            return null;

        var memberIds = team.Members.Select(m => m.PlayerId).ToList();
        var playerDict = team.Members.ToDictionary(m => m.PlayerId, m => m.Player);

        var teamMatches = await LoadTeamMatchesAsync(teamId, ct);
        var matchData = await LoadMatchDataAsync(memberIds, ct);

        var partnerSynergies = CalculatePartnerSynergies(memberIds, playerDict, matchData);
        var mapWinRates = CalculateMapWinRates(teamMatches);
        var compositionWinRates = CalculateCompositionWinRates(team, teamMatches);
        var overallScore = CalculateOverallScore(partnerSynergies);

        return CreateTeamSynergyDto(team, partnerSynergies, mapWinRates, compositionWinRates, overallScore);
    }

    private async Task<Team?> LoadTeamAsync(long teamId, CancellationToken ct)
    {
        return await dbContext.Teams
            .Include(t => t.Members)
                .ThenInclude(m => m.Player)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);
    }

    private async Task<List<TeamMatch>> LoadTeamMatchesAsync(long teamId, CancellationToken ct)
    {
        return await dbContext.TeamMatches
            .Include(tm => tm.Match)
            .Where(tm => tm.TeamId == teamId)
            .ToListAsync(ct);
    }

    private async Task<MatchData> LoadMatchDataAsync(List<long> memberIds, CancellationToken ct)
    {
        var allMatchResults = await dbContext.MatchResults
            .Where(mr => memberIds.Contains(mr.PlayerId))
            .Select(mr => new MatchResultData
            {
                MatchId = mr.MatchId,
                PlayerId = mr.PlayerId,
                Team = mr.Team,
                IsWinner = mr.IsWinner,
                RatingBefore = mr.RatingBefore
            })
            .ToListAsync(ct);

        var matchResultsByMatchAndTeam = allMatchResults
            .GroupBy(mr => new MatchTeamKey { MatchId = mr.MatchId, Team = mr.Team })
            .ToDictionary(g => g.Key, g => g.ToList());

        return new MatchData
        {
            AllMatchResults = allMatchResults,
            MatchResultsByMatchAndTeam = matchResultsByMatchAndTeam
        };
    }

    private static List<PartnerSynergyDto> CalculatePartnerSynergies(
        List<long> memberIds,
        Dictionary<long, Player> playerDict,
        MatchData matchData)
    {
        var partnerSynergies = new List<PartnerSynergyDto>();

        for (int i = 0; i < memberIds.Count; i++)
        {
            for (int j = i + 1; j < memberIds.Count; j++)
            {
                var player1Id = memberIds[i];
                var player2Id = memberIds[j];

                if (!TryGetPlayers(playerDict, player1Id, player2Id, out var player1, out var player2))
                    continue;

                var synergy = CalculatePartnerSynergy(
                    player1Id, player1, player2Id, player2, matchData);
                partnerSynergies.Add(synergy);
            }
        }

        return partnerSynergies;
    }

    private static bool TryGetPlayers(
        Dictionary<long, Player> playerDict,
        long player1Id,
        long player2Id,
        out Player player1,
        out Player player2)
    {
        if (playerDict.TryGetValue(player1Id, out player1) &&
            playerDict.TryGetValue(player2Id, out player2))
        {
            return true;
        }

        player1 = null!;
        player2 = null!;
        return false;
    }

    private static PartnerSynergyDto CalculatePartnerSynergy(
        long player1Id,
        Player player1,
        long player2Id,
        Player player2,
        MatchData matchData)
    {
        var togetherMatches = FindMatchesTogether(player1Id, player2Id, matchData.AllMatchResults);

        if (!togetherMatches.Any())
        {
            return CreateEmptyPartnerSynergy(player1Id, player1.Name, player2Id, player2.Name);
        }

        var stats = CalculateMatchStatsTogether(
            player1Id, player2Id, togetherMatches, matchData);
        var synergyScore = CalculateSynergyScore(stats.Wins, togetherMatches.Count);

        return CreatePartnerSynergyDto(
            player1Id, player1.Name, player2Id, player2.Name,
            togetherMatches.Count, stats.Wins, stats.WinRate, stats.AverageRating, synergyScore);
    }

    private static List<long> FindMatchesTogether(
        long player1Id,
        long player2Id,
        List<MatchResultData> allMatchResults)
    {
        var player1Matches = allMatchResults
            .Where(mr => mr.PlayerId == player1Id)
            .Select(mr => new { mr.MatchId, mr.Team })
            .Distinct()
            .ToList();

        var player2Matches = allMatchResults
            .Where(mr => mr.PlayerId == player2Id)
            .Select(mr => new { mr.MatchId, mr.Team })
            .Distinct()
            .ToList();

        return player1Matches
            .Join(player2Matches,
                p1 => new { p1.MatchId, p1.Team },
                p2 => new { p2.MatchId, p2.Team },
                (p1, p2) => p1.MatchId)
            .Distinct()
            .ToList();
    }

    private static MatchStatsTogether CalculateMatchStatsTogether(
        long player1Id,
        long player2Id,
        List<long> togetherMatches,
        MatchData matchData)
    {
        var wins = 0;
        var totalRating = 0.0;
        var ratingCount = 0;

        foreach (var matchId in togetherMatches)
        {
            var matchStats = GetMatchStatsForPlayers(
                matchId, player1Id, player2Id, matchData);
            
            if (matchStats.HasData)
            {
                if (matchStats.IsWin)
                    wins++;

                if (matchStats.HasRating)
                {
                    totalRating += matchStats.AverageRating;
                    ratingCount++;
                }
            }
        }

        var avgRating = ratingCount > 0 ? totalRating / ratingCount : 0.0;
        var winRate = togetherMatches.Count > 0
            ? Math.Round(wins * 100.0 / togetherMatches.Count, 2)
            : 0.0;

        return new MatchStatsTogether
        {
            Wins = wins,
            WinRate = winRate,
            AverageRating = avgRating
        };
    }

    private static MatchStatsForPlayers GetMatchStatsForPlayers(
        long matchId,
        long player1Id,
        long player2Id,
        MatchData matchData)
    {
        var player1Result = matchData.AllMatchResults.FirstOrDefault(mr =>
            mr.MatchId == matchId && mr.PlayerId == player1Id);
        
        if (player1Result == null)
            return new MatchStatsForPlayers { HasData = false };

        var matchKey = new MatchTeamKey { MatchId = matchId, Team = player1Result.Team };
        
        if (!matchData.MatchResultsByMatchAndTeam.TryGetValue(matchKey, out var matchResults))
            return new MatchStatsForPlayers { HasData = false };

        var isWin = matchResults.Any(mr => mr.IsWinner);
        var matchRatings = matchResults
            .Where(mr => mr.PlayerId == player1Id || mr.PlayerId == player2Id)
            .Select(mr => (double)mr.RatingBefore)
            .ToList();

        var hasRating = matchRatings.Any();
        var avgRating = hasRating ? matchRatings.Average() : 0.0;

        return new MatchStatsForPlayers
        {
            HasData = true,
            IsWin = isWin,
            HasRating = hasRating,
            AverageRating = avgRating
        };
    }

    private static PartnerSynergyDto CreateEmptyPartnerSynergy(
        long player1Id, string player1Name, long player2Id, string player2Name)
    {
        return new PartnerSynergyDto
        {
            Player1Id = player1Id,
            Player1Name = player1Name,
            Player2Id = player2Id,
            Player2Name = player2Name,
            MatchesTogether = 0,
            WinsTogether = 0,
            WinRateTogether = 0.0,
            AverageRatingTogether = 0.0,
            SynergyScore = 0.0
        };
    }

    private static PartnerSynergyDto CreatePartnerSynergyDto(
        long player1Id, string player1Name,
        long player2Id, string player2Name,
        int matchesTogether, int wins, double winRate, double avgRating, double synergyScore)
    {
        return new PartnerSynergyDto
        {
            Player1Id = player1Id,
            Player1Name = player1Name,
            Player2Id = player2Id,
            Player2Name = player2Name,
            MatchesTogether = matchesTogether,
            WinsTogether = wins,
            WinRateTogether = winRate,
            AverageRatingTogether = Math.Round(avgRating, 0),
            SynergyScore = synergyScore
        };
    }

    private static Dictionary<string, double> CalculateMapWinRates(List<TeamMatch> teamMatches)
    {
        return teamMatches
            .GroupBy(tm => tm.Match.MapName)
            .ToDictionary(
                g => g.Key,
                g => g.Count > 0 ? Math.Round(g.Count(tm => tm.IsWin) * 100.0 / g.Count, 2) : 0.0
            );
    }

    private static Dictionary<string, double> CalculateCompositionWinRates(
        Team team, List<TeamMatch> teamMatches)
    {
        var compositionWinRates = new Dictionary<string, double>();
        
        if (!team.Members.Any())
            return compositionWinRates;

        var classes = team.Members.Select(m => m.Player.Class).OrderBy(c => c).ToList();
        var composition = string.Join("-", classes);
        compositionWinRates[composition] = teamMatches.Count > 0
            ? Math.Round(teamMatches.Count(tm => tm.IsWin) * 100.0 / teamMatches.Count, 2)
            : 0.0;

        return compositionWinRates;
    }

    private static double CalculateOverallScore(List<PartnerSynergyDto> partnerSynergies)
    {
        return partnerSynergies.Any()
            ? Math.Round(partnerSynergies.Average(p => p.SynergyScore), 2)
            : 0.0;
    }

    private static TeamSynergyDto CreateTeamSynergyDto(
        Team team,
        List<PartnerSynergyDto> partnerSynergies,
        Dictionary<string, double> mapWinRates,
        Dictionary<string, double> compositionWinRates,
        double overallScore)
    {
        return new TeamSynergyDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            PartnerSynergies = partnerSynergies,
            MapWinRates = mapWinRates,
            CompositionWinRates = compositionWinRates,
            OverallSynergyScore = overallScore
        };
    }

    public async Task<PartnerSynergyDto?> GetPartnerSynergyAsync(long player1Id, long player2Id, CancellationToken ct = default)
    {
        var player1 = await dbContext.Players.FindAsync([player1Id], ct);
        var player2 = await dbContext.Players.FindAsync([player2Id], ct);

        if (player1 == null || player2 == null)
            return null;

        var player1Matches = await dbContext.MatchResults
            .Where(mr => mr.PlayerId == player1Id)
            .Select(mr => new { mr.MatchId, mr.Team })
            .ToListAsync(ct);

        var player2Matches = await dbContext.MatchResults
            .Where(mr => mr.PlayerId == player2Id)
            .Select(mr => new { mr.MatchId, mr.Team })
            .ToListAsync(ct);

        var togetherMatches = player1Matches
            .Join(player2Matches,
                p1 => new { p1.MatchId, p1.Team },
                p2 => new { p2.MatchId, p2.Team },
                (p1, p2) => p1.MatchId)
            .Distinct()
            .ToList();

        if (!togetherMatches.Any())
        {
            return new PartnerSynergyDto
            {
                Player1Id = player1Id,
                Player1Name = player1.Name,
                Player2Id = player2Id,
                Player2Name = player2.Name,
                MatchesTogether = 0,
                WinsTogether = 0,
                WinRateTogether = 0.0,
                AverageRatingTogether = 0.0,
                SynergyScore = 0.0
            };
        }

        var togetherResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => togetherMatches.Contains(mr.MatchId) &&
                        (mr.PlayerId == player1Id || mr.PlayerId == player2Id))
            .ToListAsync(ct);

        var wins = togetherResults
            .GroupBy(mr => mr.MatchId)
            .Count(g => g.Any(mr => mr.IsWinner));

        var avgRating = togetherResults
            .GroupBy(mr => mr.MatchId)
            .Select(g => g.Average(mr => (double)mr.RatingBefore))
            .DefaultIfEmpty(0)
            .Average();

        var winRate = togetherMatches.Count > 0
            ? Math.Round(wins * 100.0 / togetherMatches.Count, 2)
            : 0.0;

        var synergyScore = CalculateSynergyScore(wins, togetherMatches.Count);

        return new PartnerSynergyDto
        {
            Player1Id = player1Id,
            Player1Name = player1.Name,
            Player2Id = player2Id,
            Player2Name = player2.Name,
            MatchesTogether = togetherMatches.Count,
            WinsTogether = wins,
            WinRateTogether = winRate,
            AverageRatingTogether = Math.Round(avgRating, 0),
            SynergyScore = synergyScore
        };
    }

    private static double CalculateSynergyScore(int wins, int totalMatches)
    {
        if (totalMatches == 0) return 0;
        var winRate = wins / (double)totalMatches;
        var baseScore = winRate * 100;
        var matchBonus = Math.Min(totalMatches / 10.0, 10); // Cap bonus at 10
        return Math.Round(baseScore + matchBonus, 2);
    }
}

internal class MatchData
{
    public required List<MatchResultData> AllMatchResults { get; init; }
    public required Dictionary<MatchTeamKey, List<MatchResultData>> MatchResultsByMatchAndTeam { get; init; }
}

internal class MatchTeamKey
{
    public long MatchId { get; init; }
    public string Team { get; init; } = string.Empty;

    public override bool Equals(object? obj)
    {
        return obj is MatchTeamKey other &&
               MatchId == other.MatchId &&
               Team == other.Team;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MatchId, Team);
    }
}

internal class MatchResultData
{
    public long MatchId { get; init; }
    public long PlayerId { get; init; }
    public string Team { get; init; } = string.Empty;
    public bool IsWinner { get; init; }
    public int RatingBefore { get; init; }
}

internal class MatchStatsTogether
{
    public int Wins { get; init; }
    public double WinRate { get; init; }
    public double AverageRating { get; init; }
}

internal class MatchStatsForPlayers
{
    public bool HasData { get; init; }
    public bool IsWin { get; init; }
    public bool HasRating { get; init; }
    public double AverageRating { get; init; }
}

