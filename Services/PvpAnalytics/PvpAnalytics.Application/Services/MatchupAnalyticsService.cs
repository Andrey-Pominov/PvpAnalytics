using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IMatchupAnalyticsService
{
    Task<MatchupAnalyticsDto?> GetMatchupAsync(
        string class1, string? spec1, string class2, string? spec2,
        GameMode? gameMode = null, int? ratingMin = null, int? ratingMax = null,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken ct = default);
    Task<PlayerMatchupSummaryDto> GetPlayerMatchupSummaryAsync(long playerId, CancellationToken ct = default);
}

public class MatchupAnalyticsService(PvpAnalyticsDbContext dbContext) : IMatchupAnalyticsService
{
    public async Task<MatchupAnalyticsDto?> GetMatchupAsync(
        string class1, string? spec1, string class2, string? spec2,
        GameMode? gameMode = null, int? ratingMin = null, int? ratingMax = null,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var dto = new MatchupAnalyticsDto
        {
            Class1 = class1,
            Spec1 = spec1,
            Class2 = class2,
            Spec2 = spec2,
            GameMode = gameMode,
            RatingMin = ratingMin,
            RatingMax = ratingMax,
            StartDate = startDate,
            EndDate = endDate
        };

        // Find matches where class1/spec1 faced class2/spec2
        var matchesQuery = dbContext.MatchResults
            .Include(mr => mr.Match)
            .Include(mr => mr.Player)
            .AsQueryable();

        // Filter by date range
        if (startDate.HasValue)
        {
            matchesQuery = matchesQuery.Where(mr => mr.Match.CreatedOn >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            matchesQuery = matchesQuery.Where(mr => mr.Match.CreatedOn <= endDate.Value);
        }

        // Filter by game mode
        if (gameMode.HasValue)
        {
            matchesQuery = matchesQuery.Where(mr => mr.Match.GameMode == gameMode.Value);
        }

        // Get all match results
        var allResults = await matchesQuery.ToListAsync(ct);

        // Find matches where class1 and class2 faced each other
        var matchupMatches = allResults
            .GroupBy(mr => mr.MatchId)
            .Where(g =>
            {
                var teams = g.GroupBy(mr => mr.Team).ToList();
                if (teams.Count < 2) return false;

                // Check if one team has class1 and other has class2
                foreach (var team1 in teams)
                {
                    foreach (var team2 in teams.Where(t => t.Key != team1.Key))
                    {
                        var hasClass1 = team1.Any(mr =>
                            mr.Player.Class.Equals(class1, StringComparison.OrdinalIgnoreCase) &&
                            (spec1 == null || mr.Spec == spec1));
                        var hasClass2 = team2.Any(mr =>
                            mr.Player.Class.Equals(class2, StringComparison.OrdinalIgnoreCase) &&
                            (spec2 == null || mr.Spec == spec2));

                        if (hasClass1 && hasClass2)
                            return true;
                    }
                }
                return false;
            })
            .Select(g => g.Key)
            .ToList();

        if (!matchupMatches.Any())
            return dto;

        // Get match results for these matches
        var matchupResults = allResults
            .Where(mr => matchupMatches.Contains(mr.MatchId))
            .ToList();

        // Filter by rating
        if (ratingMin.HasValue || ratingMax.HasValue)
        {
            matchupResults = matchupResults.Where(mr =>
                (!ratingMin.HasValue || mr.RatingBefore >= ratingMin.Value) &&
                (!ratingMax.HasValue || mr.RatingBefore <= ratingMax.Value))
                .ToList();
        }

        // Group by match to determine winners
        var matchGroups = matchupResults.GroupBy(mr => mr.MatchId).ToList();
        dto.TotalMatches = matchGroups.Count;

        var class1Wins = 0;
        var class2Wins = 0;

        foreach (var matchGroup in matchGroups)
        {
            var teams = matchGroup.GroupBy(mr => mr.Team).ToList();
            var class1Team = teams.FirstOrDefault(t => t.Any(mr =>
                mr.Player.Class.Equals(class1, StringComparison.OrdinalIgnoreCase) &&
                (spec1 == null || mr.Spec == spec1)));
            var class2Team = teams.FirstOrDefault(t => t.Any(mr =>
                mr.Player.Class.Equals(class2, StringComparison.OrdinalIgnoreCase) &&
                (spec2 == null || mr.Spec == spec2)));

            if (class1Team != null && class2Team != null)
            {
                if (class1Team.Any(mr => mr.IsWinner))
                    class1Wins++;
                else if (class2Team.Any(mr => mr.IsWinner))
                    class2Wins++;
            }
        }

        dto.WinsForClass1 = class1Wins;
        dto.WinsForClass2 = class2Wins;
        dto.WinRateForClass1 = dto.TotalMatches > 0 ? Math.Round(class1Wins * 100.0 / dto.TotalMatches, 2) : 0;
        dto.WinRateForClass2 = dto.TotalMatches > 0 ? Math.Round(class2Wins * 100.0 / dto.TotalMatches, 2) : 0;

        // Calculate average match duration
        var matchIds = matchupMatches;
        var matches = await dbContext.Matches
            .Where(m => matchIds.Contains(m.Id))
            .ToListAsync(ct);
        dto.AverageMatchDuration = matches.Any() ? Math.Round(matches.Average(m => (double)m.Duration), 2) : 0;

        // Calculate damage/healing stats
        var class1PlayerIds = matchupResults
            .Where(mr => mr.Player.Class.Equals(class1, StringComparison.OrdinalIgnoreCase) &&
                        (spec1 == null || mr.Spec == spec1))
            .Select(mr => mr.PlayerId)
            .Distinct()
            .ToList();

        var class2PlayerIds = matchupResults
            .Where(mr => mr.Player.Class.Equals(class2, StringComparison.OrdinalIgnoreCase) &&
                        (spec2 == null || mr.Spec == spec2))
            .Select(mr => mr.PlayerId)
            .Distinct()
            .ToList();

        var combatLogs = await dbContext.CombatLogEntries
            .Where(c => matchIds.Contains(c.MatchId))
            .ToListAsync(ct);

        var class1Logs = combatLogs.Where(c => class1PlayerIds.Contains(c.SourcePlayerId)).ToList();
        var class2Logs = combatLogs.Where(c => class2PlayerIds.Contains(c.SourcePlayerId)).ToList();

        dto.Stats = new MatchupDamageHealingStats
        {
            AverageDamageClass1 = class1Logs.Any() ? Math.Round(class1Logs.Average(c => (double)c.DamageDone), 2) : 0,
            AverageDamageClass2 = class2Logs.Any() ? Math.Round(class2Logs.Average(c => (double)c.DamageDone), 2) : 0,
            AverageHealingClass1 = class1Logs.Any() ? Math.Round(class1Logs.Average(c => (double)c.HealingDone), 2) : 0,
            AverageHealingClass2 = class2Logs.Any() ? Math.Round(class2Logs.Average(c => (double)c.HealingDone), 2) : 0,
            AverageCCClass1 = class1Logs.Any() ? Math.Round(class1Logs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)matchIds.Count, 2) : 0,
            AverageCCClass2 = class2Logs.Any() ? Math.Round(class2Logs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)matchIds.Count, 2) : 0
        };

        return dto;
    }

    public async Task<PlayerMatchupSummaryDto> GetPlayerMatchupSummaryAsync(long playerId, CancellationToken ct = default)
    {
        var player = await dbContext.Players.FindAsync([playerId], ct);
        if (player == null)
            return new PlayerMatchupSummaryDto { PlayerId = playerId };

        var dto = new PlayerMatchupSummaryDto
        {
            PlayerId = playerId,
            PlayerName = player.Name
        };

        // Get all match results for this player
        var playerResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId)
            .ToListAsync(ct);

        if (!playerResults.Any())
            return dto;

        var matchIds = playerResults.Select(mr => mr.MatchId).Distinct().ToList();
        var playerTeams = playerResults.ToDictionary(mr => mr.MatchId, mr => mr.Team);

        // Get opponent results
        var opponentResults = await dbContext.MatchResults
            .Include(mr => mr.Player)
            .Where(mr => matchIds.Contains(mr.MatchId) &&
                        (!playerTeams.ContainsKey(mr.MatchId) || playerTeams[mr.MatchId] != mr.Team))
            .ToListAsync(ct);

        // Group by opponent class/spec
        var matchups = opponentResults
            .GroupBy(mr => new { mr.Player.Class, mr.Spec })
            .Select(g =>
            {
                var playerWon = g.Count(mr =>
                {
                    var playerTeam = playerTeams.GetValueOrDefault(mr.MatchId);
                    return playerTeam != null && playerTeam != mr.Team && !mr.IsWinner;
                });
                return new MatchupWinRate
                {
                    OpponentClass = g.Key.Class ?? "Unknown",
                    OpponentSpec = g.Key.Spec,
                    Matches = g.Count(),
                    Wins = playerWon,
                    WinRate = g.Count() > 0 ? Math.Round(playerWon * 100.0 / g.Count(), 2) : 0
                };
            })
            .Where(m => m.Matches >= 3) // Minimum matches for meaningful data
            .ToList();

        dto.Weaknesses = matchups
            .OrderBy(m => m.WinRate)
            .ThenByDescending(m => m.Matches)
            .Take(10)
            .ToList();

        dto.Strengths = matchups
            .OrderByDescending(m => m.WinRate)
            .ThenByDescending(m => m.Matches)
            .Take(10)
            .ToList();

        return dto;
    }
}

