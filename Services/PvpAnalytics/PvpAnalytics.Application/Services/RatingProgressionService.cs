using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Enum;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IRatingProgressionService
{
    Task<RatingProgressionDto> GetRatingProgressionAsync(
        long playerId,
        GameMode? gameMode = null,
        string? spec = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);
    Task<RatingSummary> GetRatingSummaryAsync(long playerId, CancellationToken ct = default);
}

public class RatingProgressionService(PvpAnalyticsDbContext dbContext) : IRatingProgressionService
{
    public async Task<RatingProgressionDto> GetRatingProgressionAsync(
        long playerId,
        GameMode? gameMode = null,
        string? spec = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var player = await dbContext.Players.FindAsync([playerId], ct);
        var dto = new RatingProgressionDto
        {
            PlayerId = playerId,
            PlayerName = player?.Name ?? "Unknown",
            GameMode = gameMode,
            Spec = spec,
            StartDate = startDate,
            EndDate = endDate
        };

        var query = dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId)
            .AsQueryable();

        if (gameMode.HasValue)
        {
            query = query.Where(mr => mr.Match.GameMode == gameMode.Value);
        }

        if (!string.IsNullOrWhiteSpace(spec))
        {
            query = query.Where(mr => mr.Spec == spec);
        }

        if (startDate.HasValue)
        {
            query = query.Where(mr => mr.Match.CreatedOn >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(mr => mr.Match.CreatedOn <= endDate.Value);
        }

        var matchResults = await query
            .OrderBy(mr => mr.Match.CreatedOn)
            .Select(mr => new
            {
                mr.MatchId,
                mr.Match.CreatedOn,
                mr.RatingBefore,
                mr.RatingAfter,
                mr.IsWinner,
                mr.Spec,
                mr.Match.GameMode
            })
            .ToListAsync(ct);

        dto.DataPoints = matchResults.Select(mr => new RatingDataPoint
        {
            MatchDate = mr.CreatedOn,
            MatchId = mr.MatchId,
            RatingBefore = mr.RatingBefore,
            RatingAfter = mr.RatingAfter,
            RatingChange = mr.RatingAfter - mr.RatingBefore,
            IsWinner = mr.IsWinner,
            Spec = mr.Spec,
            GameMode = mr.GameMode
        }).ToList();

        // Calculate summary
        if (dto.DataPoints.Count != 0)
        {
            dto.Summary = new RatingSummary
            {
                CurrentRating = dto.DataPoints[^1].RatingAfter,
                PeakRating = dto.DataPoints.Max(dp => Math.Max(dp.RatingBefore, dp.RatingAfter)),
                LowestRating = dto.DataPoints.Min(dp => Math.Min(dp.RatingBefore, dp.RatingAfter)),
                AverageRating = Math.Round(dto.DataPoints.Average(dp => (double)dp.RatingAfter), 2),
                TotalRatingGain = dto.DataPoints.Where(dp => dp.RatingChange > 0).Sum(dp => dp.RatingChange),
                TotalRatingLoss = Math.Abs(dto.DataPoints.Where(dp => dp.RatingChange < 0).Sum(dp => dp.RatingChange)),
                NetRatingChange = dto.DataPoints.Sum(dp => dp.RatingChange)
            };
        }
        else
        {
            dto.Summary = new RatingSummary();
        }

        return dto;
    }

    public async Task<RatingSummary> GetRatingSummaryAsync(long playerId, CancellationToken ct = default)
    {
        var matchResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId)
            .OrderBy(mr => mr.Match.CreatedOn)
            .Select(mr => new
            {
                mr.RatingBefore,
                mr.RatingAfter,
                mr.Match.CreatedOn
            })
            .ToListAsync(ct);

        if (matchResults.Count == 0)
            return new RatingSummary();

        var ratings = matchResults.SelectMany(mr => new[] { mr.RatingBefore, mr.RatingAfter }).ToList();

        return new RatingSummary
        {
            CurrentRating = matchResults[^1].RatingAfter,
            PeakRating = ratings.Max(),
            LowestRating = ratings.Min(),
            AverageRating = Math.Round(ratings.Average(), 2),
            TotalRatingGain = matchResults.Where(mr => mr.RatingAfter > mr.RatingBefore)
                .Sum(mr => mr.RatingAfter - mr.RatingBefore),
            TotalRatingLoss = Math.Abs(matchResults.Where(mr => mr.RatingAfter < mr.RatingBefore)
                .Sum(mr => mr.RatingAfter - mr.RatingBefore)),
            NetRatingChange = matchResults.Sum(mr => mr.RatingAfter - mr.RatingBefore)
        };
    }
}

