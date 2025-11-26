using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IHighlightsService
{
    Task<List<FeaturedMatchDto>> GetHighlightsAsync(string period = "day", int limit = 10, CancellationToken ct = default);
    Task<FeaturedMatchDto?> FeatureMatchAsync(long matchId, string? reason, Guid? curatorUserId, CancellationToken ct = default);
}

public class HighlightsService(PvpAnalyticsDbContext dbContext) : IHighlightsService
{
    public async Task<List<FeaturedMatchDto>> GetHighlightsAsync(string period = "day", int limit = 10, CancellationToken ct = default)
    {
        var cutoffDate = period switch
        {
            "day" => DateTime.UtcNow.AddDays(-1),
            "week" => DateTime.UtcNow.AddDays(-7),
            "month" => DateTime.UtcNow.AddDays(-30),
            _ => DateTime.UtcNow.AddDays(-1)
        };

        var featured = await dbContext.FeaturedMatches
            .Include(fm => fm.Match)
            .Where(fm => fm.FeaturedAt >= cutoffDate)
            .OrderByDescending(fm => fm.Upvotes)
            .ThenByDescending(fm => fm.FeaturedAt)
            .Take(limit)
            .ToListAsync(ct);

        return featured.Select(fm => new FeaturedMatchDto
        {
            Id = fm.Id,
            MatchId = fm.MatchId,
            FeaturedAt = fm.FeaturedAt,
            Reason = fm.Reason,
            Upvotes = fm.Upvotes,
            CommentsCount = fm.CommentsCount
        }).ToList();
    }

    public async Task<FeaturedMatchDto?> FeatureMatchAsync(long matchId, string? reason, Guid? curatorUserId, CancellationToken ct = default)
    {
        var match = await dbContext.Matches.FindAsync([matchId], ct);
        if (match == null)
            return null;

        var existing = await dbContext.FeaturedMatches
            .FirstOrDefaultAsync(fm => fm.MatchId == matchId && fm.FeaturedAt >= DateTime.UtcNow.AddDays(-1), ct);

        if (existing != null)
            return null; // Already featured today

        var featured = new FeaturedMatch
        {
            MatchId = matchId,
            FeaturedAt = DateTime.UtcNow,
            Reason = reason ?? "Featured Match",
            CuratorUserId = curatorUserId,
            Upvotes = 0,
            CommentsCount = 0
        };

        dbContext.FeaturedMatches.Add(featured);
        await dbContext.SaveChangesAsync(ct);

        return new FeaturedMatchDto
        {
            Id = featured.Id,
            MatchId = matchId,
            FeaturedAt = featured.FeaturedAt,
            Reason = featured.Reason,
            Upvotes = 0,
            CommentsCount = 0
        };
    }
}

