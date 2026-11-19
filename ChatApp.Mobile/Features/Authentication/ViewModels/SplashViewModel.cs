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
        Title = "BetweenUs";
    }

    // Runs after the app is already on screen. Only an explicit 401 (and a failed refresh) signs the
    // user out; being offline or a server hiccup keeps the session so the app still opens like WhatsApp.
    private async Task ValidateSessionInBackgroundAsync(string accessToken, string? refreshToken)
    {
        try
        {
            var profile = await _authService.GetCurrentUserAsync(accessToken);
            if (profile.IsSuccess && profile.Value != null)
            {
                _authStateService.UpdateCurrentUser(profile.Value);
                return;
            }

            if (profile.StatusCode != 401) return;

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshed = await _authService.RefreshTokenAsync(refreshToken);
                if (refreshed.IsSuccess && refreshed.Value != null)
                {
                    var tokens = refreshed.Value;
                    var newProfile = await _authService.GetCurrentUserAsync(tokens.AccessToken);
                    if (newProfile.IsSuccess && newProfile.Value != null)
                    {
                        _authStateService.SetAuthenticated(newProfile.Value, tokens.AccessToken, tokens.RefreshToken);
                        return;
                    }
                }
                else if (refreshed.StatusCode is not (400 or 401 or 403))
                {
                    // Network / server problem while refreshing - don't sign the user out for that.
                    return;
                }
            }

            _authStateService.SetUnauthenticated();
            await _navigationService.NavigateToAsync(AppRoutes.Welcome);
        }
        catch
        {
            // Never let a background check crash the app.
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "جاري التحقق من حالة الجلسة...";

        try
        {
            // No artificial splash delay: the native splash already covers app start-up.
            var accessToken = await _storageService.GetAccessTokenAsync();
            var refreshToken = await _storageService.GetRefreshTokenAsync();

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Fast path (WhatsApp-style): open the chats list immediately with the last known
                // profile and verify the session with the server in the background.
                var cachedProfile = CachedProfileStore.Load();
                if (cachedProfile != null)
                {
                    _authStateService.SetAuthenticated(cachedProfile, accessToken, refreshToken ?? "");
                    await _navigationService.NavigateToAsync(AppRoutes.MainConversations);
                    _ = ValidateSessionInBackgroundAsync(accessToken, refreshToken);
                    return;
                }

                StatusMessage = "جاري الاتصال بالسيرفر...";
                var profileResult = await _authService.GetCurrentUserAsync(accessToken);

                if (profileResult.IsSuccess && profileResult.Value != null)
                {
                    _authStateService.SetAuthenticated(profileResult.Value, accessToken, refreshToken ?? "");
                    await _navigationService.NavigateToAsync(AppRoutes.MainConversations);
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
                            await _navigationService.NavigateToAsync(AppRoutes.MainConversations);
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
