using ChatApp.Domain.Entities;

namespace ChatApp.Application.Features.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    string HashToken(string token);
}
