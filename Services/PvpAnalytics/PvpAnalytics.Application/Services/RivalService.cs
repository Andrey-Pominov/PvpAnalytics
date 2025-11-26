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

        var result = new List<RivalDto>();

        foreach (var rival in rivals)
        {
            // Calculate match statistics against this rival
            var playerMatches = await dbContext.MatchResults
                .Where(mr => mr.PlayerId == rival.OpponentPlayerId)
                .Select(mr => mr.MatchId)
                .Distinct()
                .ToListAsync(ct);

            // Get matches where user's players faced this rival
            // This is a simplified version - in reality, you'd need to track which player belongs to the user
            var matchesPlayed = playerMatches.Count;
            var wins = 0; // Would need user's player IDs to calculate properly
            var losses = matchesPlayed - wins;
            var winRate = matchesPlayed > 0 ? Math.Round(wins * 100.0 / matchesPlayed, 2) : 0.0;

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
        var player = await playerRepo.GetByIdAsync(dto.PlayerId, ct);
        if (player == null)
            return null;

        var existing = await dbContext.Rivals
            .FirstOrDefaultAsync(r => r.OwnerUserId == userId && r.OpponentPlayerId == dto.PlayerId, ct);

        if (existing != null)
            return null; // Already a rival

        var rival = new Rival
        {
            OwnerUserId = userId,
            OpponentPlayerId = dto.PlayerId,
            Notes = dto.Notes,
            IntensityScore = Math.Clamp(dto.IntensityScore, 1, 10),
            CreatedAt = DateTime.UtcNow
        };

        await rivalRepo.AddAsync(rival, ct);

        return new RivalDto
        {
            Id = rival.Id,
            OpponentPlayerId = dto.PlayerId,
            OpponentPlayerName = player.Name,
            Realm = player.Realm,
            Class = player.Class,
            Spec = player.Spec,
            Notes = rival.Notes,
            IntensityScore = rival.IntensityScore,
            CreatedAt = rival.CreatedAt,
            MatchesPlayed = 0,
            Wins = 0,
            Losses = 0,
            WinRate = 0.0
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
            Wins = 0,
            Losses = 0,
            WinRate = 0.0
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

