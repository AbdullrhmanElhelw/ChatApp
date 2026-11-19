using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public record OnboardingSlide(
    string TitleLine1,
    string TitleLine2,
    string Subtitle,
    string ImageSource,
    bool IsLogoSlide
);

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial string CurrentThemeName { get; set; } = "الوضع الداكن";

    [ObservableProperty]
    public partial int CurrentSlideIndex { get; set; }

    public ObservableCollection<OnboardingSlide> Slides { get; } = new()
    {
        new OnboardingSlide(
            "عالمكم الصغير،",
            "معاً.",
            "مساحة خاصة ومحادثات فورية تجمعكم أنتم الاتنين في مكان واحد.",
            "betweenus_logo.png",
            true),
        new OnboardingSlide(
            "خلّي لحظاتكم",
            "قريبة.",
            "سجّلوا تواريخكم الخاصة، ألبوم ذكرياتكم، ورسائل الحب المؤجلة.",
            "betweenus_splash.png",
            false),
        new OnboardingSlide(
            "تواصلوا بالصوت،",
            "القلب والمزاج.",
            "رسائل صوتية، نبضات حب متحركة، وتخصيص كامل لكل تفاصيل شاتكم.",
            "betweenus_logo.png",
            true)
    };

    public WelcomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "BetweenUs";
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
        CurrentThemeName = ThemeManager.IsDarkMode ? "داكن" : "فاتح";
    }
}
