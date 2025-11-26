using AuthService.Application.Abstractions;
using AuthService.Application.DTOs;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Services;

public class ProfileService(AuthDbContext dbContext) : IProfileService
{
    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.FindAsync([userId], ct);
        if (user == null)
            return null;

        return new ProfileDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            IsProfilePublic = user.IsProfilePublic,
            ShowStatsToFriendsOnly = user.ShowStatsToFriendsOnly
        };
    }

    public async Task<ProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await dbContext.Users.FindAsync([userId], ct);
        if (user == null)
            return null;

        if (dto.DisplayName != null)
            user.DisplayName = dto.DisplayName;

        if (dto.Bio != null)
            user.Bio = dto.Bio;

        if (dto.AvatarUrl != null)
            user.AvatarUrl = dto.AvatarUrl;

        if (dto.IsProfilePublic.HasValue)
            user.IsProfilePublic = dto.IsProfilePublic.Value;

        if (dto.ShowStatsToFriendsOnly.HasValue)
            user.ShowStatsToFriendsOnly = dto.ShowStatsToFriendsOnly.Value;

        await dbContext.SaveChangesAsync(ct);

        return new ProfileDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            IsProfilePublic = user.IsProfilePublic,
            ShowStatsToFriendsOnly = user.ShowStatsToFriendsOnly
        };
    }
}

