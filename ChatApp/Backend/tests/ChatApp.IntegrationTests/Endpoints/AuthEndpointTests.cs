using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Auth.DTOs;
using ChatApp.Application.Features.Users.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChatApp.IntegrationTests.Endpoints;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated_AndTokens()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Integration Test User",
            $"user_{uniqueId}",
            $"user_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be(registerDto.UserName);
        result.Data.Email.Should().Be(registerDto.Email);
        result.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var firstUser = new RegisterRequestDto(
            "First User",
            $"user_{uniqueId}_1",
            $"dup_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var secondUserSameEmail = new RegisterRequestDto(
            "Second User",
            $"user_{uniqueId}_2",
            $"dup_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        // Act
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", firstUser);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", secondUserSameEmail);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await secondResponse.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task Register_WithDuplicateUserName_ReturnsBadRequest()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var firstUser = new RegisterRequestDto(
            "First User",
            $"dupname_{uniqueId}",
            $"email1_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var secondUserSameName = new RegisterRequestDto(
            "Second User",
            $"dupname_{uniqueId}",
            $"email2_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        // Act
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", firstUser);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", secondUserSameName);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await secondResponse.Content.ReadAsStringAsync();
        content.Should().Contain("already taken");
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterRequestDto(
            "Weak User",
            "weak_user_123",
            "weak@example.com",
            "weak", // Fails minimum length and complexity
            "weak"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidEmailCredentials_ReturnsOk_AndTokens()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Login User",
            $"login_{uniqueId}",
            $"login_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginDto = new LoginRequestDto(registerDto.Email, "P@ssword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithValidUserNameCredentials_ReturnsOk_AndTokens()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Login User",
            $"loginname_{uniqueId}",
            $"loginname_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        var loginDto = new LoginRequestDto(registerDto.UserName, "P@ssword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data!.UserName.Should().Be(registerDto.UserName);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Wrong Password User",
            $"wrongpw_{uniqueId}",
            $"wrongpw_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        var loginDto = new LoginRequestDto(registerDto.Email, "WrongPassword999!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesTokensSuccessfully()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Refresh User",
            $"refresh_{uniqueId}",
            $"refresh_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        var regResult = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        var originalRefreshToken = regResult!.Data!.RefreshToken;

        var refreshDto = new RefreshTokenRequestDto(originalRefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBe(originalRefreshToken); // Rotated!
    }

    [Fact]
    public async Task RefreshToken_ReusingRevokedToken_ReturnsUnauthorized_SecurityAlert()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Reused Token User",
            $"reuse_{uniqueId}",
            $"reuse_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        var regResult = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        var initialRefreshToken = regResult!.Data!.RefreshToken;

        // First rotation (valid)
        var refreshDto = new RefreshTokenRequestDto(initialRefreshToken);
        var firstRotationResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshDto);
        firstRotationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Attempt to reuse the already-rotated (now revoked) token!
        var reuseResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshDto);

        // Assert
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await reuseResponse.Content.ReadAsStringAsync();
        content.Should().Contain("revoked refresh token");
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsProfile()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Profile User",
            $"profile_{uniqueId}",
            $"profile_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        var regResult = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        var token = regResult!.Data!.AccessToken;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profileResult = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>(JsonOptions);
        profileResult.Should().NotBeNull();
        profileResult!.Success.Should().BeTrue();
        profileResult.Data!.UserName.Should().Be(registerDto.UserName);
        profileResult.Data.Email.Should().Be(registerDto.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            "Logout User",
            $"logout_{uniqueId}",
            $"logout_{uniqueId}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        var regResult = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        var token = regResult!.Data!.AccessToken;
        var refreshToken = regResult.Data.RefreshToken;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new RefreshTokenRequestDto(refreshToken))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
