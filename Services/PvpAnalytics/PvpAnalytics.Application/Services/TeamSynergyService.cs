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
        var team = await dbContext.Teams
            .Include(t => t.Members)
                .ThenInclude(m => m.Player)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team == null || team.Members.Count < 2)
            return null;

        var memberIds = team.Members.Select(m => m.PlayerId).ToList();
        var playerDict = team.Members.ToDictionary(m => m.PlayerId, m => m.Player);

        // Get all matches where team members played together
        var teamMatches = await dbContext.TeamMatches
            .Include(tm => tm.Match)
            .Where(tm => tm.TeamId == teamId)
            .ToListAsync(ct);

        // Batch load all match results for all team members in a single query
        var allMatchResults = await dbContext.MatchResults
            .Where(mr => memberIds.Contains(mr.PlayerId))
            .Select(mr => new
            {
                mr.MatchId,
                mr.PlayerId,
                mr.Team,
                mr.IsWinner,
                mr.RatingBefore
            })
            .ToListAsync(ct);

        // Group match results by match ID and team for efficient lookup
        var matchResultsByMatchAndTeam = allMatchResults
            .GroupBy(mr => new { mr.MatchId, mr.Team })
            .ToDictionary(g => g.Key, g => g.ToList());

        // Calculate partner synergies in memory
        var partnerSynergies = new List<PartnerSynergyDto>();
        for (int i = 0; i < memberIds.Count; i++)
        {
            for (int j = i + 1; j < memberIds.Count; j++)
            {
                var player1Id = memberIds[i];
                var player2Id = memberIds[j];
                
                if (!playerDict.TryGetValue(player1Id, out var player1) || 
                    !playerDict.TryGetValue(player2Id, out var player2))
                    continue;

                // Find matches where both players were on the same team
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

                var togetherMatches = player1Matches
                    .Join(player2Matches,
                        p1 => new { p1.MatchId, p1.Team },
                        p2 => new { p2.MatchId, p2.Team },
                        (p1, p2) => p1.MatchId)
                    .Distinct()
                    .ToList();

                if (!togetherMatches.Any())
                {
                    partnerSynergies.Add(new PartnerSynergyDto
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
                    });
                    continue;
                }

                // Calculate wins and average rating from pre-loaded data
                var wins = 0;
                var totalRating = 0.0;
                var ratingCount = 0;

                foreach (var matchId in togetherMatches)
                {
                    // Find the team they played together on
                    var player1Result = allMatchResults.FirstOrDefault(mr => 
                        mr.MatchId == matchId && mr.PlayerId == player1Id);
                    if (player1Result == null) continue;

                    var teamName = player1Result.Team;
                    var matchKey = new { MatchId = matchId, Team = teamName };
                    
                    if (matchResultsByMatchAndTeam.TryGetValue(matchKey, out var matchResults))
                    {
                        // Check if any player in this match/team combination won
                        if (matchResults.Any(mr => mr.IsWinner))
                            wins++;

                        // Calculate average rating for this match
                        var matchRatings = matchResults
                            .Where(mr => mr.PlayerId == player1Id || mr.PlayerId == player2Id)
                            .Select(mr => (double)mr.RatingBefore)
                            .ToList();
                        
                        if (matchRatings.Any())
                        {
                            totalRating += matchRatings.Average();
                            ratingCount++;
                        }
                    }
                }

                var avgRating = ratingCount > 0 ? totalRating / ratingCount : 0.0;
                var winRate = togetherMatches.Count > 0
                    ? Math.Round(wins * 100.0 / togetherMatches.Count, 2)
                    : 0.0;
                var synergyScore = CalculateSynergyScore(wins, togetherMatches.Count);

                partnerSynergies.Add(new PartnerSynergyDto
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
                });
            }
        }

        // Calculate map win rates
        var mapWinRates = teamMatches
            .GroupBy(tm => tm.Match.MapName)
            .ToDictionary(
                g => g.Key,
                g => g.Count() > 0 ? Math.Round(g.Count(tm => tm.IsWin) * 100.0 / g.Count(), 2) : 0.0
            );

        // Calculate composition win rates (based on team members' classes)
        var compositionWinRates = new Dictionary<string, double>();
        if (team.Members.Any())
        {
            var classes = team.Members.Select(m => m.Player.Class).OrderBy(c => c).ToList();
            var composition = string.Join("-", classes);
            var compMatches = teamMatches;
            compositionWinRates[composition] = compMatches.Count > 0
                ? Math.Round(compMatches.Count(tm => tm.IsWin) * 100.0 / compMatches.Count, 2)
                : 0.0;
        }

        // Calculate overall synergy score
        var overallScore = partnerSynergies.Any()
            ? Math.Round(partnerSynergies.Average(p => p.SynergyScore), 2)
            : 0.0;

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

        // Find matches where both players were on the same team
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

        // Get match results for these matches
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

        // Calculate synergy score
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

