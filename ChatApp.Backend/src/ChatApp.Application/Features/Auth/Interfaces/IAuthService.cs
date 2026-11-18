using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Auth.DTOs;

namespace ChatApp.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
