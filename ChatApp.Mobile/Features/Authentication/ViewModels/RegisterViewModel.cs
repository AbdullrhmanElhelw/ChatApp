using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthApiService _authService;
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial bool IsConfirmPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial string? FullNameError { get; set; }

    [ObservableProperty]
    public partial string? UserNameError { get; set; }

    [ObservableProperty]
    public partial string? EmailError { get; set; }

    [ObservableProperty]
    public partial string? PasswordError { get; set; }

    [ObservableProperty]
    public partial string? ConfirmPasswordError { get; set; }

    public bool CanSubmit => !IsBusy &&
        !string.IsNullOrWhiteSpace(FullName) &&
        !string.IsNullOrWhiteSpace(UserName) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(ConfirmPassword);

    public RegisterViewModel(
        IAuthApiService authService,
        IAuthenticationStateService authStateService,
        INavigationService navigationService)
    {
        _authService = authService;
        _authStateService = authStateService;
        _navigationService = navigationService;
        Title = "إنشاء حساب جديد";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordHidden = !IsConfirmPasswordHidden;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        ClearErrors();

        // 1. Client-Side Validation
        bool hasError = false;

        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length < 2)
        {
            FullNameError = "يرجى كتابة الاسم الكامل (حرفين على الأقل)";
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(UserName) || UserName.Trim().Length < 3)
        {
            UserNameError = "اسم المستخدم يجب ألا يقل عن 3 أحرف";
            hasError = true;
        }
        else if (!Regex.IsMatch(UserName.Trim(), @"^[a-zA-Z0-9_]+$"))
        {
            UserNameError = "اسم المستخدم يقبل فقط الحروف الإنجليزية والأرقام وعلامة _";
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "يرجى إدخال البريد الإلكتروني";
            hasError = true;
        }
        else if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            EmailError = "صيغة البريد الإلكتروني غير صحيحة";
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            PasswordError = "كلمة المرور يجب أن تكون 6 خانات على الأقل وبها حرف كبير ورقم ورمز";
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordError = "يرجى تأكيد كلمة المرور";
            hasError = true;
        }
        else if (Password != ConfirmPassword)
        {
            ConfirmPasswordError = "كلمتا المرور غير متطابقتين";
            hasError = true;
        }

        if (hasError) return;

        // 2. Submit to Backend API
        IsBusy = true;
        try
        {
            var request = new RegisterRequestDto(
                FullName.Trim(),
                UserName.Trim(),
                Email.Trim(),
                Password,
                ConfirmPassword
            );

            var result = await _authService.RegisterAsync(request);

            if (result.IsSuccess && result.Value != null)
            {
                var authData = result.Value;
                var userProfile = new UserProfileDto(
                    authData.UserId,
                    authData.UserName,
                    authData.Email,
                    authData.FullName,
                    null,
                    null,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow
                );

                _authStateService.SetAuthenticated(userProfile, authData.AccessToken, authData.RefreshToken);

                // Navigate to Profile Setup screen
                await _navigationService.NavigateToAsync(AppRoutes.ProfileSetup);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "فشل إنشاء الحساب. يرجى مراجعة البيانات.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        ClearErrors();
        await _navigationService.NavigateToAsync(AppRoutes.Login);
    }

    [RelayCommand]
    private async Task NavigateBackAsync()
    {
        ClearErrors();
        await _navigationService.NavigateToAsync(AppRoutes.Welcome);
    }

    private void ClearErrors()
    {
        ErrorMessage = null;
        FullNameError = null;
        UserNameError = null;
        EmailError = null;
        PasswordError = null;
        ConfirmPasswordError = null;
    }
}
