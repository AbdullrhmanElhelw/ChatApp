namespace ChatApp.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    Guid UserId,
    string UserName,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
