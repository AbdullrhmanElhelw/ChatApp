using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Settings.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;
    private readonly IAuthApiService? _authService;
    private readonly IChatHubService? _chatHubService;

    [ObservableProperty]
    public partial UserProfileDto? CurrentUser { get; set; }

    [ObservableProperty]
    public partial bool IsDarkMode { get; set; }

    [ObservableProperty]
    public partial string CurrentMoodLabel { get; set; } = "💜 عالمكم الصغير";

    public SettingsViewModel(
        IAuthenticationStateService authStateService,
        INavigationService navigationService,
        IAuthApiService? authService = null,
        IChatHubService? chatHubService = null)
    {
        _authStateService = authStateService;
        _navigationService = navigationService;
        _authService = authService;
        _chatHubService = chatHubService;

        Title = "الإعدادات";
        RefreshState();
    }

    public void RefreshState()
    {
        CurrentUser = _authStateService.CurrentUser;
        IsDarkMode = ThemeManager.IsDarkMode;
        CurrentMoodLabel = Preferences.Default.Get("betweenus_mood", "💜 عالمكم الصغير");
    }

    [RelayCommand]
    private async Task NavigateBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.ProfileSetup);
    }

    [RelayCommand]
    private async Task OpenStickerStudioAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.StickerStudio);
    }

    [RelayCommand]
    private void ToggleDarkMode()
    {
        ThemeManager.ToggleTheme();
        IsDarkMode = ThemeManager.IsDarkMode;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            if (_chatHubService != null)
            {
                await _chatHubService.DisconnectAsync();
            }

            if (_authService != null)
            {
                await _authService.LogoutAsync();
            }
        }
        catch
        {
            // Proceed with local sign-out even if network revocation fails
        }

        _authStateService.SetUnauthenticated();
        await _navigationService.NavigateToAsync(AppRoutes.Welcome);
    }
}
