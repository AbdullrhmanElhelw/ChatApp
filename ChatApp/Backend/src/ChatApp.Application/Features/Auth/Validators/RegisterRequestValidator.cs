using System.Text.RegularExpressions;
using ChatApp.Application.Features.Auth.DTOs;
using FluentValidation;

namespace ChatApp.Application.Features.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly Regex PasswordUpperRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex PasswordLowerRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex PasswordDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex PasswordSpecialRegex = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);
    private static readonly Regex UserNameRegex = new(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled);

    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.")
            .Matches(UserNameRegex).WithMessage("Username can only contain alphanumeric characters, underscores, and dots.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Must(p => PasswordUpperRegex.IsMatch(p)).WithMessage("Password must contain at least one uppercase letter.")
            .Must(p => PasswordLowerRegex.IsMatch(p)).WithMessage("Password must contain at least one lowercase letter.")
            .Must(p => PasswordDigitRegex.IsMatch(p)).WithMessage("Password must contain at least one digit.")
            .Must(p => PasswordSpecialRegex.IsMatch(p)).WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
