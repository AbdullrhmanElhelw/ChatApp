using ChatApp.Mobile.Features.Authentication.DTOs;

namespace ChatApp.Mobile.Features.Authentication.Services;

public class AuthenticationStateService : IAuthenticationStateService
{
    private readonly ISecureStorageService _storageService;

    public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(AccessToken);
    public UserProfileDto? CurrentUser { get; private set; }
    public string? AccessToken { get; private set; }

    public event Action<bool>? AuthenticationStateChanged;

    public AuthenticationStateService(ISecureStorageService storageService)
    {
        _storageService = storageService;
    }

    public void SetAuthenticated(UserProfileDto user, string accessToken, string refreshToken)
    {
        CurrentUser = user;
        AccessToken = accessToken;
        _ = _storageService.SaveTokensAsync(accessToken, refreshToken);
        _ = _storageService.SaveUserDataAsync(user.Id, user.UserName, user.Email, user.FullName);
        AuthenticationStateChanged?.Invoke(true);
    }

    public void UpdateCurrentUser(UserProfileDto user)
    {
        CurrentUser = user;
        _ = _storageService.SaveUserDataAsync(user.Id, user.UserName, user.Email, user.FullName);
    }

    public void SetUnauthenticated()
    {
        CurrentUser = null;
        AccessToken = null;
        _ = _storageService.ClearAllAsync();
        AuthenticationStateChanged?.Invoke(false);
    }

    public async Task InitializeAsync()
    {
        AccessToken = await _storageService.GetAccessTokenAsync();
        var userId = await _storageService.GetUserIdAsync();
        var userName = await _storageService.GetUserNameAsync();
        var fullName = await _storageService.GetFullNameAsync();

        if (userId.HasValue && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(AccessToken))
        {
            CurrentUser = new UserProfileDto(
                userId.Value,
                userName,
                "",
                fullName ?? userName,
                null,
                null,
                true,
                DateTime.UtcNow,
                DateTime.UtcNow
            );
        }
    }
}
