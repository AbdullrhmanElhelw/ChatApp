using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Users.DTOs;

namespace ChatApp.Application.Features.Users.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserProfileDto>> GetCurrentUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ApiResponse<UserProfileDto>> UpdateCurrentUserProfileAsync(
        Guid userId,
        UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserProfileDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResponse<UserProfileDto>> SearchUsersAsync(
        Guid currentUserId,
        string? query,
        int pageIndex = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task UpdatePresenceAsync(Guid userId, bool isOnline, CancellationToken cancellationToken = default);
}
