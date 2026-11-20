using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Authentication.ViewModels;
using ChatApp.Mobile.Navigation;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.ViewModels;

public class RegisterViewModelTests
{
    private readonly Mock<IAuthApiService> _authServiceMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly RegisterViewModel _sut;

    public RegisterViewModelTests()
    {
        _authServiceMock = new Mock<IAuthApiService>();
        _authStateMock = new Mock<IAuthenticationStateService>();
        _navigationMock = new Mock<INavigationService>();

        _sut = new RegisterViewModel(_authServiceMock.Object, _authStateMock.Object, _navigationMock.Object);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ShouldSetConfirmPasswordErrorAndNotCallApi()
    {
        // Arrange
        _sut.FullName = "John Doe";
        _sut.UserName = "johndoe";
        _sut.Email = "john@example.com";
        _sut.Password = "Password123!";
        _sut.ConfirmPassword = "DifferentPassword123!";

        // Act
        await _sut.RegisterCommand.ExecuteAsync(null);

        // Assert
        _sut.ConfirmPasswordError.Should().Be("كلمتا المرور غير متطابقتين");
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldSetEmailErrorAndNotCallApi()
    {
        // Arrange
        _sut.FullName = "John Doe";
        _sut.UserName = "johndoe";
        _sut.Email = "invalid-email";
        _sut.Password = "Password123!";
        _sut.ConfirmPassword = "Password123!";

        // Act
        await _sut.RegisterCommand.ExecuteAsync(null);

        // Assert
        _sut.EmailError.Should().Be("صيغة البريد الإلكتروني غير صحيحة");
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithShortUserName_ShouldSetUserNameErrorAndNotCallApi()
    {
        // Arrange
        _sut.FullName = "John Doe";
        _sut.UserName = "ab";
        _sut.Email = "john@example.com";
        _sut.Password = "Password123!";
        _sut.ConfirmPassword = "Password123!";

        // Act
        await _sut.RegisterCommand.ExecuteAsync(null);

        // Assert
        _sut.UserNameError.Should().Be("اسم المستخدم يجب ألا يقل عن 3 أحرف");
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCallApiAndNavigateToProfileSetup()
    {
        // Arrange
        _sut.FullName = "John Doe";
        _sut.UserName = "johndoe";
        _sut.Email = "john@example.com";
        _sut.Password = "Password123!";
        _sut.ConfirmPassword = "Password123!";

        var authResponse = new AuthResponseDto(
            Guid.NewGuid(),
            "johndoe",
            "john@example.com",
            "John Doe",
            "access_token_abc",
            "refresh_token_xyz",
            DateTime.UtcNow.AddMinutes(15)
        );

        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync(ApiResult<AuthResponseDto>.Success(authResponse, 201));

        // Act
        await _sut.RegisterCommand.ExecuteAsync(null);

        // Assert
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterRequestDto>()), Times.Once);
        _authStateMock.Verify(x => x.SetAuthenticated(It.IsAny<UserProfileDto>(), "access_token_abc", "refresh_token_xyz"), Times.Once);
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.ProfileSetup, null), Times.Once);
        _sut.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Register_WhenBackendReturnsConflict_ShouldSetErrorMessage()
    {
        // Arrange
        _sut.FullName = "John Doe";
        _sut.UserName = "duplicate_user";
        _sut.Email = "duplicate@example.com";
        _sut.Password = "Password123!";
        _sut.ConfirmPassword = "Password123!";

        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync(ApiResult<AuthResponseDto>.Failure("اسم المستخدم 'duplicate_user' مستخدم بالفعل.", 409));

        // Act
        await _sut.RegisterCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().Be("اسم المستخدم 'duplicate_user' مستخدم بالفعل.");
        _navigationMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()), Times.Never);
    }
}
