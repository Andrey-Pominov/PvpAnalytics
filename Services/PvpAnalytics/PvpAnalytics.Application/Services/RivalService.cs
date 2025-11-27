using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IRivalService
{
    Task<List<RivalDto>> GetRivalsAsync(Guid userId, CancellationToken ct = default);
    Task<RivalDto?> AddRivalAsync(Guid userId, CreateRivalDto dto, CancellationToken ct = default);
    Task<RivalDto?> UpdateRivalAsync(Guid userId, long rivalId, UpdateRivalDto dto, CancellationToken ct = default);
    Task<bool> RemoveRivalAsync(Guid userId, long rivalId, CancellationToken ct = default);
}

public class RivalService(
    IRepository<Rival> rivalRepo,
    IRepository<Player> playerRepo,
    PvpAnalyticsDbContext dbContext) : IRivalService
{
    public async Task<List<RivalDto>> GetRivalsAsync(Guid userId, CancellationToken ct = default)
    {
        var rivals = await dbContext.Rivals
            .Include(r => r.OpponentPlayer)
            .Where(r => r.OwnerUserId == userId)
            .OrderByDescending(r => r.IntensityScore)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        // Get user's player IDs (using FavoritePlayer as a proxy for user-owned players)
        var userPlayerIds = await dbContext.FavoritePlayers
            .Where(fp => fp.OwnerUserId == userId)
            .Select(fp => fp.TargetPlayerId)
            .ToListAsync(ct);

        var result = new List<RivalDto>();

        foreach (var rival in rivals)
        {
            // Calculate match statistics against this rival
            var matchesPlayed = 0;
            int? wins = null;
            int? losses = null;
            double? winRate = null;

            // Only calculate stats if we have user's player IDs
            if (userPlayerIds.Count > 0)
            {
                // Get matches where both the rival's player and user's players participated
                var matchesWithRival = await dbContext.MatchResults
                    .Where(mr => mr.PlayerId == rival.OpponentPlayerId)
                    .Select(mr => mr.MatchId)
                    .Distinct()
                    .ToListAsync(ct);

                // Get matches where user's players also participated
                var matchesWithBoth = await dbContext.MatchResults
                    .Where(mr => matchesWithRival.Contains(mr.MatchId) && userPlayerIds.Contains(mr.PlayerId))
                    .Select(mr => mr.MatchId)
                    .Distinct()
                    .ToListAsync(ct);

                matchesPlayed = matchesWithBoth.Count;

                if (matchesPlayed > 0)
                {
                    // Count wins: matches where a user's player won against the rival
                    wins = await dbContext.MatchResults
                        .Where(mr => matchesWithBoth.Contains(mr.MatchId) 
                            && userPlayerIds.Contains(mr.PlayerId) 
                            && mr.IsWinner)
                        .Select(mr => mr.MatchId)
                        .Distinct()
                        .CountAsync(ct);

                    losses = matchesPlayed - wins;
                    winRate = Math.Round(wins.Value * 100.0 / matchesPlayed, 2);
                }
                else
                {
                    wins = 0;
                    losses = 0;
                    winRate = 0.0;
                }
            }
            else
            {
                // If no user players available, just count total matches with rival
                matchesPlayed = await dbContext.MatchResults
                    .Where(mr => mr.PlayerId == rival.OpponentPlayerId)
                    .Select(mr => mr.MatchId)
                    .Distinct()
                    .CountAsync(ct);
            }

            result.Add(new RivalDto
            {
                Id = rival.Id,
                OpponentPlayerId = rival.OpponentPlayerId,
                OpponentPlayerName = rival.OpponentPlayer.Name,
                Realm = rival.OpponentPlayer.Realm,
                Class = rival.OpponentPlayer.Class,
                Spec = rival.OpponentPlayer.Spec,
                Notes = rival.Notes,
                IntensityScore = rival.IntensityScore,
                CreatedAt = rival.CreatedAt,
                MatchesPlayed = matchesPlayed,
                Wins = wins,
                Losses = losses,
                WinRate = winRate
            });
        }

        return result;
    }

    public async Task<RivalDto?> AddRivalAsync(Guid userId, CreateRivalDto dto, CancellationToken ct = default)
    {
        var player = await playerRepo.GetByIdAsync(dto.OpponentPlayerId, ct);
        if (player == null)
            return null;

        var existing = await dbContext.Rivals
            .FirstOrDefaultAsync(r => r.OwnerUserId == userId && r.OpponentPlayerId == dto.OpponentPlayerId, ct);

        if (existing != null)
            return null; // Already a rival

        var rival = new Rival
        {
            OwnerUserId = userId,
            OpponentPlayerId = dto.OpponentPlayerId,
            Notes = dto.Notes,
            IntensityScore = Math.Clamp(dto.IntensityScore, 1, 10),
            CreatedAt = DateTime.UtcNow
        };

        await rivalRepo.AddAsync(rival, ct);

        return new RivalDto
        {
            Id = rival.Id,
            OpponentPlayerId = dto.OpponentPlayerId,
            OpponentPlayerName = player.Name,
            Realm = player.Realm,
            Class = player.Class,
            Spec = player.Spec,
            Notes = rival.Notes,
            IntensityScore = rival.IntensityScore,
            CreatedAt = rival.CreatedAt,
            MatchesPlayed = 0,
            Wins = null,
            Losses = null,
            WinRate = null
        };
    }

    public async Task<RivalDto?> UpdateRivalAsync(Guid userId, long rivalId, UpdateRivalDto dto, CancellationToken ct = default)
    {
        var rival = await dbContext.Rivals
            .Include(r => r.OpponentPlayer)
            .FirstOrDefaultAsync(r => r.Id == rivalId && r.OwnerUserId == userId, ct);

        if (rival == null)
            return null;

        if (dto.Notes != null)
            rival.Notes = dto.Notes;

        if (dto.IntensityScore.HasValue)
            rival.IntensityScore = Math.Clamp(dto.IntensityScore.Value, 1, 10);

        await rivalRepo.UpdateAsync(rival, ct);

        return new RivalDto
        {
            Id = rival.Id,
            OpponentPlayerId = rival.OpponentPlayerId,
            OpponentPlayerName = rival.OpponentPlayer.Name,
            Realm = rival.OpponentPlayer.Realm,
            Class = rival.OpponentPlayer.Class,
            Spec = rival.OpponentPlayer.Spec,
            Notes = rival.Notes,
            IntensityScore = rival.IntensityScore,
            CreatedAt = rival.CreatedAt,
            MatchesPlayed = 0,
            Wins = null,
            Losses = null,
            WinRate = null
        };
    }

    public async Task<bool> RemoveRivalAsync(Guid userId, long rivalId, CancellationToken ct = default)
    {
        var rival = await dbContext.Rivals
            .FirstOrDefaultAsync(r => r.Id == rivalId && r.OwnerUserId == userId, ct);

        if (rival == null)
            return false;

        await rivalRepo.DeleteAsync(rival, ct);
        return true;
    }
}

