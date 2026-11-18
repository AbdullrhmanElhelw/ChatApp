using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Users.DTOs;

namespace ChatApp.Application.Features.Users.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserProfileDto>> GetCurrentUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
