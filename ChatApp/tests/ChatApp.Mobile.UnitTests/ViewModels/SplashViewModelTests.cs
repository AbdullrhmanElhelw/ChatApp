using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Authentication.ViewModels;
using ChatApp.Mobile.Navigation;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.ViewModels;

public class SplashViewModelTests
{
    private readonly Mock<IAuthApiService> _authServiceMock;
    private readonly Mock<ISecureStorageService> _storageServiceMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly SplashViewModel _sut;

    public SplashViewModelTests()
    {
        _authServiceMock = new Mock<IAuthApiService>();
        _storageServiceMock = new Mock<ISecureStorageService>();
        _authStateMock = new Mock<IAuthenticationStateService>();
        _navigationMock = new Mock<INavigationService>();

        _sut = new SplashViewModel(
            _authServiceMock.Object,
            _storageServiceMock.Object,
            _authStateMock.Object,
            _navigationMock.Object
        );
    }

    [Fact]
    public async Task Initialize_WhenNoTokensExist_ShouldNavigateToWelcome()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync((string?)null);
        _storageServiceMock.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync((string?)null);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.Welcome, null), Times.Once);
        _authStateMock.Verify(x => x.SetAuthenticated(It.IsAny<UserProfileDto>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Initialize_WhenValidTokenExists_ShouldSetAuthenticatedAndNavigateToMain()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync("valid_access_token");
        _storageServiceMock.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync("valid_refresh_token");

        var profile = new UserProfileDto(
            Guid.NewGuid(),
            "ahmed",
            "ahmed@example.com",
            "Ahmed Mohamed",
            null,
            null,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync("valid_access_token"))
            .ReturnsAsync(ApiResult<UserProfileDto>.Success(profile));

        // Act
        await _sut.InitializeAsync();

        // Assert
        _authStateMock.Verify(x => x.SetAuthenticated(profile, "valid_access_token", "valid_refresh_token"), Times.Once);
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.MainConversations, null), Times.Once);
    }

    [Fact]
    public async Task Initialize_WhenTokenExpired_ShouldRefreshAndNavigateToMain()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync("expired_token");
        _storageServiceMock.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync("valid_refresh_token");

        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync("expired_token"))
            .ReturnsAsync(ApiResult<UserProfileDto>.Failure("Unauthorized", 401));

        var newAuth = new AuthResponseDto(
            Guid.NewGuid(),
            "ahmed",
            "ahmed@example.com",
            "Ahmed Mohamed",
            "new_access_token",
            "new_refresh_token",
            DateTime.UtcNow.AddMinutes(15)
        );

        _authServiceMock
            .Setup(x => x.RefreshTokenAsync("valid_refresh_token"))
            .ReturnsAsync(ApiResult<AuthResponseDto>.Success(newAuth));

        var refreshedProfile = new UserProfileDto(
            newAuth.UserId,
            newAuth.UserName,
            newAuth.Email,
            newAuth.FullName,
            null,
            null,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync("new_access_token"))
            .ReturnsAsync(ApiResult<UserProfileDto>.Success(refreshedProfile));

        // Act
        await _sut.InitializeAsync();

        // Assert
        _authStateMock.Verify(x => x.SetAuthenticated(refreshedProfile, "new_access_token", "new_refresh_token"), Times.Once);
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.MainConversations, null), Times.Once);
    }
}
