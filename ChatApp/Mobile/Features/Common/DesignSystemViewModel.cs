using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatApp.Mobile.Features.Common;

public partial class DesignSystemViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string DemoInputText { get; set; } = "مرحباً بكم في ChatApp!";

    [ObservableProperty]
    public partial string DemoPassword { get; set; } = "Password123!";

    [ObservableProperty]
    public partial bool IsActionLoading { get; set; }

    [ObservableProperty]
    public partial int SampleUnreadCount { get; set; } = 14;

    [ObservableProperty]
    public partial string CurrentThemeName { get; set; } = "الوضع الافتراضي";

    public DesignSystemViewModel()
    {
        Title = "ChatApp Design System";
        UpdateThemeName();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeManager.ToggleTheme();
        UpdateThemeName();
    }

    [RelayCommand]
    private async Task SimulateLoadingActionAsync()
    {
        IsActionLoading = true;
        await Task.Delay(2000);
        IsActionLoading = false;
        SampleUnreadCount++;
    }

    private void UpdateThemeName()
    {
        CurrentThemeName = Application.Current?.UserAppTheme == AppTheme.Dark
            ? "الوضع الليلي (Dark 🌙)"
            : "الوضع النهاري (Light ☀️)";
    }
}
