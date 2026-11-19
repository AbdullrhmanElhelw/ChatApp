using ChatApp.Mobile.Features.Authentication.DTOs;

namespace ChatApp.Mobile.Features.Authentication.Services;

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; }

    public static ApiResult<T> Success(T value, int statusCode = 200) =>
        new() { IsSuccess = true, Value = value, StatusCode = statusCode };

    public static ApiResult<T> Failure(string errorMessage, int statusCode = 400) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };
}

public interface IAuthApiService
{
    Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<ApiResult<bool>> LogoutAsync();
    Task<ApiResult<UserProfileDto>> GetCurrentUserAsync(string? accessToken = null);
}
