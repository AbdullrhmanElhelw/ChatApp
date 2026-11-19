using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Authentication.ViewModels;
using ChatApp.Mobile.Navigation;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.ViewModels;

public class LoginViewModelTests
{
    private readonly Mock<IAuthApiService> _authServiceMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly LoginViewModel _sut;

    public LoginViewModelTests()
    {
        _authServiceMock = new Mock<IAuthApiService>();
        _authStateMock = new Mock<IAuthenticationStateService>();
        _navigationMock = new Mock<INavigationService>();

        _sut = new LoginViewModel(_authServiceMock.Object, _authStateMock.Object, _navigationMock.Object);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ShouldSetValidationErrorsAndNotCallApi()
    {
        // Arrange
        _sut.UserNameOrEmail = "";
        _sut.Password = "";

        // Act
        await _sut.LoginCommand.ExecuteAsync(null);

        // Assert
        _sut.UserNameOrEmailError.Should().NotBeNullOrEmpty();
        _sut.PasswordError.Should().NotBeNullOrEmpty();
        _authServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginRequestDto>()), Times.Never);
        _navigationMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldCallApiAndNavigateToDesignSystem()
    {
        // Arrange
        _sut.UserNameOrEmail = "john_doe";
        _sut.Password = "Password123!";

        var authResponse = new AuthResponseDto(
            Guid.NewGuid(),
            "john_doe",
            "john@example.com",
            "John Doe",
            "access_token_123",
            "refresh_token_456",
            DateTime.UtcNow.AddMinutes(15)
        );

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(ApiResult<AuthResponseDto>.Success(authResponse));

        // Act
        await _sut.LoginCommand.ExecuteAsync(null);

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(It.Is<LoginRequestDto>(r => r.UserNameOrEmail == "john_doe" && r.Password == "Password123!")), Times.Once);
        _authStateMock.Verify(x => x.SetAuthenticated(It.IsAny<UserProfileDto>(), "access_token_123", "refresh_token_456"), Times.Once);
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.DesignSystem, null), Times.Once);
        _sut.ErrorMessage.Should().BeNull();
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WhenApiFails_ShouldSetErrorMessageAndStayOnPage()
    {
        // Arrange
        _sut.UserNameOrEmail = "john_doe";
        _sut.Password = "WrongPassword!";

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(ApiResult<AuthResponseDto>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة.", 401));

        // Act
        await _sut.LoginCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().Be("اسم المستخدم أو كلمة المرور غير صحيحة.");
        _authStateMock.Verify(x => x.SetAuthenticated(It.IsAny<UserProfileDto>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _navigationMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()), Times.Never);
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void TogglePasswordVisibility_ShouldFlipIsPasswordHidden()
    {
        // Arrange
        _sut.IsPasswordHidden = true;

        // Act
        _sut.TogglePasswordVisibilityCommand.Execute(null);

        // Assert
        _sut.IsPasswordHidden.Should().BeFalse();

        // Act again
        _sut.TogglePasswordVisibilityCommand.Execute(null);

        // Assert
        _sut.IsPasswordHidden.Should().BeTrue();
    }
}
