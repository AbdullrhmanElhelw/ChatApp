using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public partial class SplashViewModel : BaseViewModel
{
    private readonly IAuthApiService _authService;
    private readonly ISecureStorageService _storageService;
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "جاري التحميل...";

    public SplashViewModel(
        IAuthApiService authService,
        ISecureStorageService storageService,
        IAuthenticationStateService authStateService,
        INavigationService navigationService)
    {
        _authService = authService;
        _storageService = storageService;
        _authStateService = authStateService;
        _navigationService = navigationService;
        Title = "ChatApp";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "جاري التحقق من حالة الجلسة...";

        try
        {
            // Allow splash branding display
            await Task.Delay(1000);

            var accessToken = await _storageService.GetAccessTokenAsync();
            var refreshToken = await _storageService.GetRefreshTokenAsync();

            if (!string.IsNullOrEmpty(accessToken))
            {
                StatusMessage = "جاري الاتصال بالسيرفر...";
                var profileResult = await _authService.GetCurrentUserAsync(accessToken);

                if (profileResult.IsSuccess && profileResult.Value != null)
                {
                    _authStateService.SetAuthenticated(profileResult.Value, accessToken, refreshToken ?? "");
                    await _navigationService.NavigateToAsync(AppRoutes.DesignSystem);
                    return;
                }

                // If token expired, try refreshing
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    StatusMessage = "جاري تجديد الجلسة...";
                    var refreshResult = await _authService.RefreshTokenAsync(refreshToken);

                    if (refreshResult.IsSuccess && refreshResult.Value != null)
                    {
                        var newTokens = refreshResult.Value;
                        var newProfile = await _authService.GetCurrentUserAsync(newTokens.AccessToken);

                        if (newProfile.IsSuccess && newProfile.Value != null)
                        {
                            _authStateService.SetAuthenticated(newProfile.Value, newTokens.AccessToken, newTokens.RefreshToken);
                            await _navigationService.NavigateToAsync(AppRoutes.DesignSystem);
                            return;
                        }
                    }
                }

                // Session invalid - clean up
                await _storageService.ClearAllAsync();
            }

            // Not authenticated
            await _navigationService.NavigateToAsync(AppRoutes.Welcome);
        }
        catch (Exception ex)
        {
            StatusMessage = $"تنبيه: {ex.Message}";
            await Task.Delay(800);
            await _navigationService.NavigateToAsync(AppRoutes.Welcome);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
