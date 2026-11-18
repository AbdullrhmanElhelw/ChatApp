using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChatApp.UnitTests.Identity;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGeneratorTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "SuperSecretKeyForUnitTestingMustBeLongEnoughToMeetHmacSha256Requirements!",
            Issuer = "ChatApp.Tests",
            Audience = "ChatApp.Tests.Client",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        _tokenGenerator = new JwtTokenGenerator(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidToken_WithExpectedClaims()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            FullName = "Test User"
        };

        // Act
        var (token, expiresAt) = _tokenGenerator.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(_jwtSettings.Issuer);
        jwt.Audiences.Should().Contain(_jwtSettings.Audience);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == user.UserName);
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "fullName" && c.Value == user.FullName);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceCryptographicallyRandomString()
    {
        // Act
        var token1 = _tokenGenerator.GenerateRefreshToken();
        var token2 = _tokenGenerator.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashToken_ShouldBeDeterministicAndHexEncoded()
    {
        // Arrange
        const string rawToken = "raw_refresh_token_test_123";

        // Act
        var hash1 = _tokenGenerator.HashToken(rawToken);
        var hash2 = _tokenGenerator.HashToken(rawToken);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA-256 is 32 bytes = 64 hex characters
        hash1.Should().MatchRegex("^[a-f0-9]{64}$");
    }
}
