using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IFavoritePlayerService
{
    Task<List<FavoritePlayerDto>> GetFavoritesAsync(Guid userId, CancellationToken ct = default);
    Task<FavoritePlayerDto?> AddFavoriteAsync(Guid userId, long playerId, CancellationToken ct = default);
    Task<bool> RemoveFavoriteAsync(Guid userId, long playerId, CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(Guid userId, long playerId, CancellationToken ct = default);
}

public class FavoritePlayerService(
    IRepository<FavoritePlayer> favoriteRepo,
    IRepository<Player> playerRepo,
    PvpAnalyticsDbContext dbContext) : IFavoritePlayerService
{
    public async Task<List<FavoritePlayerDto>> GetFavoritesAsync(Guid userId, CancellationToken ct = default)
    {
        var favorites = await dbContext.FavoritePlayers
            .Include(fp => fp.TargetPlayer)
            .Where(fp => fp.OwnerUserId == userId)
            .OrderByDescending(fp => fp.CreatedAt)
            .ToListAsync(ct);

        return favorites.Select(fp => new FavoritePlayerDto
        {
            Id = fp.Id,
            TargetPlayerId = fp.TargetPlayerId,
            PlayerName = fp.TargetPlayer.Name,
            Realm = fp.TargetPlayer.Realm,
            Class = fp.TargetPlayer.Class,
            Spec = fp.TargetPlayer.Spec,
            CreatedAt = fp.CreatedAt
        }).ToList();
    }

    public async Task<FavoritePlayerDto?> AddFavoriteAsync(Guid userId, long playerId, CancellationToken ct = default)
    {
        var player = await playerRepo.GetByIdAsync(playerId, ct);
        if (player == null)
            return null;

        var existing = await dbContext.FavoritePlayers
            .FirstOrDefaultAsync(fp => fp.OwnerUserId == userId && fp.TargetPlayerId == playerId, ct);

        if (existing != null)
            return null; // Already favorited

        var favorite = new FavoritePlayer
        {
            OwnerUserId = userId,
            TargetPlayerId = playerId,
            CreatedAt = DateTime.UtcNow
        };

        await favoriteRepo.AddAsync(favorite,true, ct);

        return new FavoritePlayerDto
        {
            Id = favorite.Id,
            TargetPlayerId = playerId,
            PlayerName = player.Name,
            Realm = player.Realm,
            Class = player.Class,
            Spec = player.Spec,
            CreatedAt = favorite.CreatedAt
        };
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, long playerId, CancellationToken ct = default)
    {
        var favorite = await dbContext.FavoritePlayers
            .FirstOrDefaultAsync(fp => fp.OwnerUserId == userId && fp.TargetPlayerId == playerId, ct);

        if (favorite == null)
            return false;

        await favoriteRepo.DeleteAsync(favorite, true, ct);
        return true;
    }

    public async Task<bool> IsFavoriteAsync(Guid userId, long playerId, CancellationToken ct = default)
    {
        return await dbContext.FavoritePlayers
            .AnyAsync(fp => fp.OwnerUserId == userId && fp.TargetPlayerId == playerId, ct);
    }
}

