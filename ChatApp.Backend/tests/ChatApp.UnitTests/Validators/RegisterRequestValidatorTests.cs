using ChatApp.Application.Features.Auth.DTOs;
using ChatApp.Application.Features.Auth.Validators;
using FluentAssertions;
using Xunit;

namespace ChatApp.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var request = new RegisterRequestDto(
            "Abdullah Ahmed",
            "abdullah_99",
            "abdullah@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithEmptyFullName_ShouldFail(string fullName)
    {
        var request = new RegisterRequestDto(
            fullName,
            "valid_user",
            "user@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.FullName));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("test@")]
    [InlineData("@domain.com")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var request = new RegisterRequestDto(
            "Valid Name",
            "valid_user",
            email,
            "P@ssword123!",
            "P@ssword123!"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Theory]
    [InlineData("ab")] // Too short (< 3)
    [InlineData("user name with spaces")]
    [InlineData("user#name$")]
    public void Validate_WithInvalidUserName_ShouldFail(string userName)
    {
        var request = new RegisterRequestDto(
            "Valid Name",
            userName,
            "valid@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.UserName));
    }

    [Theory]
    [InlineData("short1!")] // < 8 characters
    [InlineData("alllowercase123!")] // No uppercase
    [InlineData("ALLUPPERCASE123!")] // No lowercase
    [InlineData("NoDigitsInPassword!")] // No digit
    [InlineData("NoSpecialChars123")] // No special character
    public void Validate_WithWeakPassword_ShouldFail(string password)
    {
        var request = new RegisterRequestDto(
            "Valid Name",
            "valid_user",
            "valid@example.com",
            password,
            password
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
    }

    [Fact]
    public void Validate_WhenPasswordsDoNotMatch_ShouldFail()
    {
        var request = new RegisterRequestDto(
            "Valid Name",
            "valid_user",
            "valid@example.com",
            "P@ssword123!",
            "DifferentPassword123!"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ConfirmPassword));
    }
}
