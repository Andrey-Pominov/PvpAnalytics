using AuthService.Application.DTOs;

namespace AuthService.Application.Abstractions;

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
}

