using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.CoupleSpace.DTOs;

namespace ChatApp.Mobile.Features.CoupleSpace.Services;

public interface ICoupleApiService
{
    Task<ApiResult<CoupleSpaceClientDto?>> GetMySpaceAsync();
    Task<ApiResult<CoupleSpaceClientDto>> CreateSpaceAsync();
    Task<ApiResult<CoupleSpaceClientDto>> JoinByCodeAsync(string inviteCode);
    Task<ApiResult<CoupleSpaceClientDto>> UpdateMoodAsync(string mood);
    Task<ApiResult<CoupleSpaceClientDto>> SyncSpaceAsync(SyncCoupleSpaceRequestClientDto request);
    Task<ApiResult<bool>> UnlinkAsync();
}
