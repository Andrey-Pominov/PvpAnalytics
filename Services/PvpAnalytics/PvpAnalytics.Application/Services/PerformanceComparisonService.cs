using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IPerformanceComparisonService
{
    Task<PerformanceComparisonDto> ComparePlayerAsync(
        long playerId,
        string spec,
        int? ratingMin = null,
        CancellationToken ct = default);
    Task<PercentileRankings> GetPlayerPercentilesAsync(
        long playerId,
        string spec,
        int? ratingMin = null,
        CancellationToken ct = default);
}

public class PerformanceComparisonService(PvpAnalyticsDbContext dbContext) : IPerformanceComparisonService
{
    public async Task<PerformanceComparisonDto> ComparePlayerAsync(
        long playerId,
        string spec,
        int? ratingMin = null,
        CancellationToken ct = default)
    {
        var player = await dbContext.Players.FindAsync([playerId], ct);
        if (player == null)
            return new PerformanceComparisonDto { PlayerId = playerId, Spec = spec };

        var dto = new PerformanceComparisonDto
        {
            PlayerId = playerId,
            PlayerName = player.Name,
            Spec = spec,
            RatingMin = ratingMin
        };

        var playerResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId && mr.Spec == spec)
            .ToListAsync(ct);

        if (!playerResults.Any())
            return dto;

        if (ratingMin.HasValue)
        {
            playerResults = playerResults.Where(mr => mr.RatingBefore >= ratingMin.Value).ToList();
        }

        var playerMatchIds = playerResults.Select(mr => mr.MatchId).Distinct().ToList();

        var playerCombatLogs = await dbContext.CombatLogEntries
            .Where(c => c.SourcePlayerId == playerId && playerMatchIds.Contains(c.MatchId))
            .ToListAsync(ct);

        var playerWins = playerResults.Count(mr => mr.IsWinner);
        var playerTotalMatches = playerResults.Count;
        dto.PlayerMetrics = new PlayerMetrics
        {
            WinRate = playerTotalMatches > 0 ? Math.Round(playerWins * 100.0 / playerTotalMatches, 2) : 0,
            AverageDamage = playerCombatLogs.Any() ? Math.Round(playerCombatLogs.Average(c => (double)c.DamageDone), 2) : 0,
            AverageHealing = playerCombatLogs.Any() ? Math.Round(playerCombatLogs.Average(c => (double)c.HealingDone), 2) : 0,
            AverageCC = playerCombatLogs.Any() ? Math.Round(playerCombatLogs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)playerMatchIds.Count, 2) : 0,
            CurrentRating = playerResults.OrderByDescending(mr => mr.Match.CreatedOn).First().RatingAfter,
            PeakRating = playerResults.Max(mr => Math.Max(mr.RatingBefore, mr.RatingAfter)),
            AverageMatchDuration = playerResults.Any() ? Math.Round(playerResults.Average(mr => (double)mr.Match.Duration), 2) : 0
        };

        var allSpecResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.Spec == spec)
            .ToListAsync(ct);

        if (ratingMin.HasValue)
        {
            allSpecResults = allSpecResults.Where(mr => mr.RatingBefore >= ratingMin.Value).ToList();
        }

        var allSpecPlayerIds = allSpecResults.Select(mr => mr.PlayerId).Distinct().ToList();
        var allSpecMatchIds = allSpecResults.Select(mr => mr.MatchId).Distinct().ToList();

        var allSpecCombatLogs = await dbContext.CombatLogEntries
            .Where(c => allSpecPlayerIds.Contains(c.SourcePlayerId) && allSpecMatchIds.Contains(c.MatchId))
            .ToListAsync(ct);

        var topPlayerWins = allSpecResults.Count(mr => mr.IsWinner);
        var topPlayerTotalMatches = allSpecResults.Count;

        dto.TopPlayerMetrics = new TopPlayerMetrics
        {
            AverageWinRate = topPlayerTotalMatches > 0 ? Math.Round(topPlayerWins * 100.0 / topPlayerTotalMatches, 2) : 0,
            AverageDamage = allSpecCombatLogs.Any() ? Math.Round(allSpecCombatLogs.Average(c => (double)c.DamageDone), 2) : 0,
            AverageHealing = allSpecCombatLogs.Any() ? Math.Round(allSpecCombatLogs.Average(c => (double)c.HealingDone), 2) : 0,
            AverageCC = allSpecCombatLogs.Any() ? Math.Round(allSpecCombatLogs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)allSpecMatchIds.Count, 2) : 0,
            AverageRating = allSpecResults.Any() ? Math.Round(allSpecResults.Average(mr => (double)mr.RatingAfter), 2) : 0,
            AverageMatchDuration = allSpecResults.Any() ? Math.Round(allSpecResults.Average(mr => (double)mr.Match.Duration), 2) : 0
        };

        dto.Gaps = new ComparisonGaps
        {
            WinRateGap = dto.PlayerMetrics.WinRate - dto.TopPlayerMetrics.AverageWinRate,
            DamageGap = dto.PlayerMetrics.AverageDamage - dto.TopPlayerMetrics.AverageDamage,
            HealingGap = dto.PlayerMetrics.AverageHealing - dto.TopPlayerMetrics.AverageHealing,
            CCGap = dto.PlayerMetrics.AverageCC - dto.TopPlayerMetrics.AverageCC,
            RatingGap = dto.PlayerMetrics.CurrentRating - dto.TopPlayerMetrics.AverageRating
        };

        if (dto.Gaps.WinRateGap > 5)
            dto.Gaps.Strengths.Add("Win Rate");
        else if (dto.Gaps.WinRateGap < -5)
            dto.Gaps.Weaknesses.Add("Win Rate");

        if (dto.Gaps.DamageGap > 10000)
            dto.Gaps.Strengths.Add("Damage Output");
        else if (dto.Gaps.DamageGap < -10000)
            dto.Gaps.Weaknesses.Add("Damage Output");

        if (dto.Gaps.HealingGap > 5000)
            dto.Gaps.Strengths.Add("Healing Output");
        else if (dto.Gaps.HealingGap < -5000)
            dto.Gaps.Weaknesses.Add("Healing Output");

        // Calculate percentiles
        dto.Percentiles = await GetPlayerPercentilesAsync(playerId, spec, ratingMin, ct);

        return dto;
    }

    public async Task<PercentileRankings> GetPlayerPercentilesAsync(
        long playerId,
        string spec,
        int? ratingMin = null,
        CancellationToken ct = default)
    {
        var playerResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId && mr.Spec == spec)
            .ToListAsync(ct);

        if (ratingMin.HasValue)
        {
            playerResults = playerResults.Where(mr => mr.RatingBefore >= ratingMin.Value).ToList();
        }

        if (!playerResults.Any())
            return new PercentileRankings();

        var playerMatchIds = playerResults.Select(mr => mr.MatchId).Distinct().ToList();
        var playerCombatLogs = await dbContext.CombatLogEntries
            .Where(c => c.SourcePlayerId == playerId && playerMatchIds.Contains(c.MatchId))
            .ToListAsync(ct);

        var playerWinRate = playerResults.Count(mr => mr.IsWinner) / (double)playerResults.Count;
        var playerDamage = playerCombatLogs.Any() ? playerCombatLogs.Average(c => (double)c.DamageDone) : 0;
        var playerHealing = playerCombatLogs.Any() ? playerCombatLogs.Average(c => (double)c.HealingDone) : 0;
        var playerCC = playerCombatLogs.Any() ? playerCombatLogs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)playerMatchIds.Count : 0;
        var playerRating = playerResults.OrderByDescending(mr => mr.Match.CreatedOn).First().RatingAfter;

        var allSpecResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.Spec == spec)
            .ToListAsync(ct);

        if (ratingMin.HasValue)
        {
            allSpecResults = allSpecResults.Where(mr => mr.RatingBefore >= ratingMin.Value).ToList();
        }

        var allSpecPlayerIds = allSpecResults.Select(mr => mr.PlayerId).Distinct().ToList();
        var allSpecMatchIds = allSpecResults.Select(mr => mr.MatchId).Distinct().ToList();

        var allPlayerStats = new List<(double winRate, double damage, double healing, double cc, int rating)>();

        foreach (var specPlayerId in allSpecPlayerIds)
        {
            var specPlayerResults = allSpecResults.Where(mr => mr.PlayerId == specPlayerId).ToList();
            var specPlayerMatchIds = specPlayerResults.Select(mr => mr.MatchId).Distinct().ToList();
            var specPlayerCombatLogs = await dbContext.CombatLogEntries
                .Where(c => c.SourcePlayerId == specPlayerId && specPlayerMatchIds.Contains(c.MatchId))
                .ToListAsync(ct);

            var winRate = specPlayerResults.Count(mr => mr.IsWinner) / (double)specPlayerResults.Count;
            var damage = specPlayerCombatLogs.Any() ? specPlayerCombatLogs.Average(c => (double)c.DamageDone) : 0;
            var healing = specPlayerCombatLogs.Any() ? specPlayerCombatLogs.Average(c => (double)c.HealingDone) : 0;
            var cc = specPlayerCombatLogs.Any() ? specPlayerCombatLogs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)specPlayerMatchIds.Count : 0;
            var rating = specPlayerResults.OrderByDescending(mr => mr.Match.CreatedOn).First().RatingAfter;

            allPlayerStats.Add((winRate, damage, healing, cc, rating));
        }

        var totalPlayers = allPlayerStats.Count;
        if (totalPlayers == 0)
            return new PercentileRankings();

        var winRatePercentile = allPlayerStats.Count(s => s.winRate <= playerWinRate) * 100.0 / totalPlayers;
        var damagePercentile = allPlayerStats.Count(s => s.damage <= playerDamage) * 100.0 / totalPlayers;
        var healingPercentile = allPlayerStats.Count(s => s.healing <= playerHealing) * 100.0 / totalPlayers;
        var ccPercentile = allPlayerStats.Count(s => s.cc <= playerCC) * 100.0 / totalPlayers;
        var ratingPercentile = allPlayerStats.Count(s => s.rating <= playerRating) * 100.0 / totalPlayers;

        return new PercentileRankings
        {
            WinRatePercentile = Math.Round(winRatePercentile, 2),
            DamagePercentile = Math.Round(damagePercentile, 2),
            HealingPercentile = Math.Round(healingPercentile, 2),
            CCPercentile = Math.Round(ccPercentile, 2),
            RatingPercentile = Math.Round(ratingPercentile, 2)
        };
    }
}

