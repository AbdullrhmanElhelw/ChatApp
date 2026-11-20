namespace ChatApp.Application.Features.Auth.DTOs;

public record LoginRequestDto(
    string UserNameOrEmail,
    string Password
);
