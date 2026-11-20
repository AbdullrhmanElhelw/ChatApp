using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.Services;

public class AuthenticationStateServiceTests
{
    private readonly Mock<ISecureStorageService> _storageServiceMock;
    private readonly AuthenticationStateService _sut;

    public AuthenticationStateServiceTests()
    {
        _storageServiceMock = new Mock<ISecureStorageService>();
        _sut = new AuthenticationStateService(_storageServiceMock.Object);
    }

    [Fact]
    public void SetAuthenticated_ShouldUpdateCurrentUserAndTokensAndRaiseEvent()
    {
        // Arrange
        bool eventFired = false;
        bool isAuthenticatedInEvent = false;
        _sut.AuthenticationStateChanged += (isAuth) =>
        {
            eventFired = true;
            isAuthenticatedInEvent = isAuth;
        };

        var user = new UserProfileDto(
            Guid.NewGuid(),
            "alice",
            "alice@example.com",
            "Alice Wonderland",
            null,
            null,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        _sut.SetAuthenticated(user, "test_access_token", "test_refresh_token");

        // Assert
        _sut.IsAuthenticated.Should().BeTrue();
        _sut.CurrentUser.Should().Be(user);
        _sut.AccessToken.Should().Be("test_access_token");
        eventFired.Should().BeTrue();
        isAuthenticatedInEvent.Should().BeTrue();

        _storageServiceMock.Verify(x => x.SaveTokensAsync("test_access_token", "test_refresh_token"), Times.Once);
        _storageServiceMock.Verify(x => x.SaveUserDataAsync(user.Id, user.UserName, user.Email, user.FullName), Times.Once);
    }

    [Fact]
    public void SetUnauthenticated_ShouldClearStateAndTokensAndRaiseEvent()
    {
        // Arrange
        var user = new UserProfileDto(
            Guid.NewGuid(),
            "alice",
            "alice@example.com",
            "Alice Wonderland",
            null,
            null,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );
        _sut.SetAuthenticated(user, "token", "refresh");

        bool eventFired = false;
        bool isAuthenticatedInEvent = true;
        _sut.AuthenticationStateChanged += (isAuth) =>
        {
            eventFired = true;
            isAuthenticatedInEvent = isAuth;
        };

        // Act
        _sut.SetUnauthenticated();

        // Assert
        _sut.IsAuthenticated.Should().BeFalse();
        _sut.CurrentUser.Should().BeNull();
        _sut.AccessToken.Should().BeNull();
        eventFired.Should().BeTrue();
        isAuthenticatedInEvent.Should().BeFalse();
        _storageServiceMock.Verify(x => x.ClearAllAsync(), Times.Once);
    }
}
