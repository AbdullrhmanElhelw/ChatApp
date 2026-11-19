using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthApiService _authService;
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string UserNameOrEmail { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial string? UserNameOrEmailError { get; set; }

    [ObservableProperty]
    public partial string? PasswordError { get; set; }

    public bool CanSubmit => !IsBusy && !string.IsNullOrWhiteSpace(UserNameOrEmail) && !string.IsNullOrWhiteSpace(Password);

    public LoginViewModel(
        IAuthApiService authService,
        IAuthenticationStateService authStateService,
        INavigationService navigationService)
    {
        _authService = authService;
        _authStateService = authStateService;
        _navigationService = navigationService;
        Title = "تسجيل الدخول";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        ClearErrors();

        // 1. Client-side Validation
        bool hasValidationError = false;
        if (string.IsNullOrWhiteSpace(UserNameOrEmail))
        {
            UserNameOrEmailError = "يرجى إدخال اسم المستخدم أو البريد الإلكتروني";
            hasValidationError = true;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordError = "يرجى إدخال كلمة المرور";
            hasValidationError = true;
        }

        if (hasValidationError) return;

        // 2. Submit to API
        IsBusy = true;
        try
        {
            var request = new LoginRequestDto(UserNameOrEmail.Trim(), Password);
            var result = await _authService.LoginAsync(request);

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
                await _navigationService.NavigateToAsync(AppRoutes.DesignSystem);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "فشل تسجيل الدخول. يرجى التحقق من بياناتك.";
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
    private async Task NavigateToRegisterAsync()
    {
        ClearErrors();
        await _navigationService.NavigateToAsync(AppRoutes.Register);
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
        UserNameOrEmailError = null;
        PasswordError = null;
    }
}
