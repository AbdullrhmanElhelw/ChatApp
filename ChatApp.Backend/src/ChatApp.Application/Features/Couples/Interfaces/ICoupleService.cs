using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Couples.DTOs;

namespace ChatApp.Application.Features.Couples.Interfaces;

public interface ICoupleService
{
    Task<ApiResponse<CoupleSpaceDto?>> GetMySpaceAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CoupleSpaceDto>> CreateSpaceAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CoupleSpaceDto>> JoinByCodeAsync(Guid currentUserId, string inviteCode, CancellationToken cancellationToken = default);
    Task<ApiResponse<CoupleSpaceDto>> UpdateMoodAsync(Guid currentUserId, string mood, CancellationToken cancellationToken = default);
    Task<ApiResponse<CoupleSpaceDto>> SyncSpaceAsync(Guid currentUserId, SyncCoupleSpaceRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> UnlinkAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
