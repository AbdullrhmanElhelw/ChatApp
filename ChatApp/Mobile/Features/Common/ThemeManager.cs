namespace ChatApp.Mobile.Features.Common;

public static class ThemeManager
{
    public static bool IsDarkMode => Application.Current?.UserAppTheme == AppTheme.Dark;

    public static void ToggleTheme()
    {
        if (Application.Current == null) return;

        Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Dark
            ? AppTheme.Light
            : AppTheme.Dark;
    }

    public static void SetTheme(AppTheme theme)
    {
        if (Application.Current == null) return;
        Application.Current.UserAppTheme = theme;
    }
}
