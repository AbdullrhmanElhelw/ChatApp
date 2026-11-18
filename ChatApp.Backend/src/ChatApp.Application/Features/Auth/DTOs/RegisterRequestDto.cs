namespace ChatApp.Application.Features.Auth.DTOs;

public record RegisterRequestDto(
    string FullName,
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
);
