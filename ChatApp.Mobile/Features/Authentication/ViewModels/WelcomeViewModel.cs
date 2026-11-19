using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial string CurrentThemeName { get; set; } = "الوضع الفاتح";

    public WelcomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "مرحباً بك في ChatApp";
        UpdateThemeName();
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.Login);
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.Register);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeManager.ToggleTheme();
        UpdateThemeName();
    }

    private void UpdateThemeName()
    {
        CurrentThemeName = ThemeManager.IsDarkMode ? "الوضع الداكن 🌙" : "الوضع الفاتح ☀️";
    }
}
