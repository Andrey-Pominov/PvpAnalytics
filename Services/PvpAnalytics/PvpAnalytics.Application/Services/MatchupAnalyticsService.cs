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
        var dto = CreateMatchupDto(class1, spec1, class2, spec2, gameMode, ratingMin, ratingMax, startDate, endDate);
        
        var allResults = await LoadMatchResultsAsync(startDate, endDate, gameMode, ct);
        var matchupMatches = FindMatchupMatches(allResults, class1, spec1, class2, spec2);
        
        if (!matchupMatches.Any())
            return dto;

        var matchupResults = FilterMatchupResults(allResults, matchupMatches, ratingMin, ratingMax);
        CalculateWinRates(dto, matchupResults, class1, spec1, class2, spec2);
        
        dto.AverageMatchDuration = await CalculateAverageMatchDurationAsync(matchupMatches, ct);
        dto.Stats = await CalculateMatchupStatsAsync(matchupMatches, matchupResults, class1, spec1, class2, spec2, ct);

        return dto;
    }

    private static MatchupAnalyticsDto CreateMatchupDto(
        string class1, string? spec1, string class2, string? spec2,
        GameMode? gameMode, int? ratingMin, int? ratingMax,
        DateTime? startDate, DateTime? endDate)
    {
        return new MatchupAnalyticsDto
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
    }

    private async Task<List<Core.Entities.MatchResult>> LoadMatchResultsAsync(
        DateTime? startDate, DateTime? endDate, GameMode? gameMode, CancellationToken ct)
    {
        var matchesQuery = dbContext.MatchResults
            .Include(mr => mr.Match)
            .Include(mr => mr.Player)
            .AsQueryable();

        matchesQuery = ApplyDateFilters(matchesQuery, startDate, endDate);
        matchesQuery = ApplyGameModeFilter(matchesQuery, gameMode);

        return await matchesQuery.ToListAsync(ct);
    }

    private static IQueryable<Core.Entities.MatchResult> ApplyDateFilters(
        IQueryable<Core.Entities.MatchResult> query,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            query = query.Where(mr => mr.Match.CreatedOn >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(mr => mr.Match.CreatedOn <= endDate.Value);
        }

        return query;
    }

    private static IQueryable<Core.Entities.MatchResult> ApplyGameModeFilter(
        IQueryable<Core.Entities.MatchResult> query,
        GameMode? gameMode)
    {
        if (gameMode.HasValue)
        {
            query = query.Where(mr => mr.Match.GameMode == gameMode.Value);
        }

        return query;
    }

    private static List<long> FindMatchupMatches(
        List<Core.Entities.MatchResult> allResults,
        string class1, string? spec1,
        string class2, string? spec2)
    {
        return allResults
            .GroupBy(mr => mr.MatchId)
            .Where(g => HasMatchupInMatch(g, class1, spec1, class2, spec2))
            .Select(g => g.Key)
            .ToList();
    }

    private static bool HasMatchupInMatch(
        IGrouping<long, Core.Entities.MatchResult> matchGroup,
        string class1, string? spec1,
        string class2, string? spec2)
    {
        var teams = matchGroup.GroupBy(mr => mr.Team).ToList();
        if (teams.Count < 2)
            return false;

        return TeamsHaveMatchup(teams, class1, spec1, class2, spec2);
    }

    private static bool TeamsHaveMatchup(
        List<IGrouping<string, Core.Entities.MatchResult>> teams,
        string class1, string? spec1,
        string class2, string? spec2)
    {
        foreach (var team1 in teams)
        {
            foreach (var team2 in teams.Where(t => t.Key != team1.Key))
            {
                if (TeamHasClass(team1, class1, spec1) && TeamHasClass(team2, class2, spec2))
                    return true;
            }
        }

        return false;
    }

    private static bool TeamHasClass(
        IGrouping<string, Core.Entities.MatchResult> team,
        string className,
        string? spec)
    {
        return team.Any(mr =>
            mr.Player.Class.Equals(className, StringComparison.OrdinalIgnoreCase) &&
            (spec == null || mr.Spec == spec));
    }

    private static List<Core.Entities.MatchResult> FilterMatchupResults(
        List<Core.Entities.MatchResult> allResults,
        List<long> matchupMatches,
        int? ratingMin,
        int? ratingMax)
    {
        var matchupResults = allResults
            .Where(mr => matchupMatches.Contains(mr.MatchId))
            .ToList();

        if (ratingMin.HasValue || ratingMax.HasValue)
        {
            matchupResults = matchupResults.Where(mr =>
                (!ratingMin.HasValue || mr.RatingBefore >= ratingMin.Value) &&
                (!ratingMax.HasValue || mr.RatingBefore <= ratingMax.Value))
                .ToList();
        }

        return matchupResults;
    }

    private static void CalculateWinRates(
        MatchupAnalyticsDto dto,
        List<Core.Entities.MatchResult> matchupResults,
        string class1, string? spec1,
        string class2, string? spec2)
    {
        var matchGroups = matchupResults.GroupBy(mr => mr.MatchId).ToList();
        dto.TotalMatches = matchGroups.Count;

        var (class1Wins, class2Wins) = CountWins(matchGroups, class1, spec1, class2, spec2);

        dto.WinsForClass1 = class1Wins;
        dto.WinsForClass2 = class2Wins;
        dto.WinRateForClass1 = CalculateWinRate(class1Wins, dto.TotalMatches);
        dto.WinRateForClass2 = CalculateWinRate(class2Wins, dto.TotalMatches);
    }

    private static (int class1Wins, int class2Wins) CountWins(
        List<IGrouping<long, Core.Entities.MatchResult>> matchGroups,
        string class1, string? spec1,
        string class2, string? spec2)
    {
        var class1Wins = 0;
        var class2Wins = 0;

        foreach (var matchGroup in matchGroups)
        {
            var teams = matchGroup.GroupBy(mr => mr.Team).ToList();
            var class1Team = FindTeamWithClass(teams, class1, spec1);
            var class2Team = FindTeamWithClass(teams, class2, spec2);

            if (class1Team == null || class2Team == null)
                continue;

            if (class1Team.Any(mr => mr.IsWinner))
                class1Wins++;
            else if (class2Team.Any(mr => mr.IsWinner))
                class2Wins++;
        }

        return (class1Wins, class2Wins);
    }

    private static IGrouping<string, Core.Entities.MatchResult>? FindTeamWithClass(
        List<IGrouping<string, Core.Entities.MatchResult>> teams,
        string className,
        string? spec)
    {
        return teams.FirstOrDefault(t => TeamHasClass(t, className, spec));
    }

    private static double CalculateWinRate(int wins, int totalMatches)
    {
        return totalMatches > 0 ? Math.Round(wins * 100.0 / totalMatches, 2) : 0;
    }

    private async Task<double> CalculateAverageMatchDurationAsync(List<long> matchIds, CancellationToken ct)
    {
        var matches = await dbContext.Matches
            .Where(m => matchIds.Contains(m.Id))
            .ToListAsync(ct);

        return matches.Count != 0 ? Math.Round(matches.Average(m => (double)m.Duration), 2) : 0;
    }

    private async Task<MatchupDamageHealingStats> CalculateMatchupStatsAsync(
        List<long> matchIds,
        List<Core.Entities.MatchResult> matchupResults,
        string class1, string? spec1,
        string class2, string? spec2,
        CancellationToken ct)
    {
        var class1PlayerIds = GetPlayerIdsForClass(matchupResults, class1, spec1);
        var class2PlayerIds = GetPlayerIdsForClass(matchupResults, class2, spec2);

        var combatLogs = await dbContext.CombatLogEntries
            .Where(c => matchIds.Contains(c.MatchId))
            .ToListAsync(ct);

        var class1Logs = combatLogs.Where(c => class1PlayerIds.Contains(c.SourcePlayerId)).ToList();
        var class2Logs = combatLogs.Where(c => class2PlayerIds.Contains(c.SourcePlayerId)).ToList();

        return CreateMatchupStats(class1Logs, class2Logs, matchIds.Count);
    }

    private static List<long> GetPlayerIdsForClass(
        List<Core.Entities.MatchResult> matchupResults,
        string className,
        string? spec)
    {
        return matchupResults
            .Where(mr => mr.Player.Class.Equals(className, StringComparison.OrdinalIgnoreCase) &&
                        (spec == null || mr.Spec == spec))
            .Select(mr => mr.PlayerId)
            .Distinct()
            .ToList();
    }

    private static MatchupDamageHealingStats CreateMatchupStats(
        List<Core.Entities.CombatLogEntry> class1Logs,
        List<Core.Entities.CombatLogEntry> class2Logs,
        int matchCount)
    {
        return new MatchupDamageHealingStats
        {
            AverageDamageClass1 = CalculateAverageDamage(class1Logs),
            AverageDamageClass2 = CalculateAverageDamage(class2Logs),
            AverageHealingClass1 = CalculateAverageHealing(class1Logs),
            AverageHealingClass2 = CalculateAverageHealing(class2Logs),
            AverageCCClass1 = CalculateAverageCC(class1Logs, matchCount),
            AverageCCClass2 = CalculateAverageCC(class2Logs, matchCount)
        };
    }

    private static double CalculateAverageDamage(List<Core.Entities.CombatLogEntry> logs)
    {
        return logs.Count != 0 ? Math.Round(logs.Average(c => (double)c.DamageDone), 2) : 0;
    }

    private static double CalculateAverageHealing(List<Core.Entities.CombatLogEntry> logs)
    {
        return logs.Count != 0 ? Math.Round(logs.Average(c => (double)c.HealingDone), 2) : 0;
    }

    private static double CalculateAverageCC(List<Core.Entities.CombatLogEntry> logs, int matchCount)
    {
        if (logs.Count == 0 || matchCount == 0)
            return 0;

        var ccCount = logs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl));
        return Math.Round(ccCount / (double)matchCount, 2);
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

