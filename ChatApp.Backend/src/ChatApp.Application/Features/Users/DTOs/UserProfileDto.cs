namespace ChatApp.Application.Features.Users.DTOs;

public record UserProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? Bio,
    string? AvatarUrl,
    bool IsOnline,
    DateTime? LastSeenAt,
    DateTime CreatedAt
);

public record UpdateUserProfileRequestDto(
    string FullName,
    string? Bio,
    string? AvatarUrl
);
