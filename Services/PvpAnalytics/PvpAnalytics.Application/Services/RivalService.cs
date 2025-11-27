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

        if (rivals.Count == 0)
            return new List<RivalDto>();

        var userPlayerIds = await dbContext.FavoritePlayers
            .Where(fp => fp.OwnerUserId == userId)
            .Select(fp => fp.TargetPlayerId)
            .ToListAsync(ct);

        var rivalOpponentIds = rivals.Select(r => r.OpponentPlayerId).ToList();

        var allPlayerIds = userPlayerIds.Union(rivalOpponentIds).ToList();
        var allMatchResults = await dbContext.MatchResults
            .Where(mr => allPlayerIds.Contains(mr.PlayerId))
            .Select(mr => new
            {
                mr.MatchId,
                mr.PlayerId,
                mr.IsWinner
            })
            .ToListAsync(ct);

        var matchResultsByMatch = allMatchResults
            .GroupBy(mr => mr.MatchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var matchesByPlayer = allMatchResults
            .GroupBy(mr => mr.PlayerId)
            .ToDictionary(g => g.Key, g => g.Select(mr => mr.MatchId).Distinct().ToHashSet());

        var result = new List<RivalDto>();

        foreach (var rival in rivals)
        {
            var matchesPlayed = 0;
            int? wins = null;
            int? losses = null;
            double? winRate = null;

            if (userPlayerIds.Count > 0)
            {
                if (!matchesByPlayer.TryGetValue(rival.OpponentPlayerId, out var rivalMatches))
                {
                    rivalMatches = new HashSet<long>();
                }

                var userMatches = userPlayerIds
                    .Where(pid => matchesByPlayer.ContainsKey(pid))
                    .SelectMany(pid => matchesByPlayer[pid])
                    .ToHashSet();

                var matchesWithBoth = rivalMatches.Intersect(userMatches).ToList();
                matchesPlayed = matchesWithBoth.Count;

                if (matchesPlayed > 0)
                {
                    wins = matchesWithBoth.Count(matchId =>
                    {
                        if (!matchResultsByMatch.TryGetValue(matchId, out var matchResults))
                            return false;

                        return matchResults.Any(mr => userPlayerIds.Contains(mr.PlayerId) && mr.IsWinner);
                    });

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
                if (matchesByPlayer.TryGetValue(rival.OpponentPlayerId, out var rivalMatches))
                {
                    matchesPlayed = rivalMatches.Count;
                }
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

