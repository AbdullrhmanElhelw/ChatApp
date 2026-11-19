using ChatApp.Mobile.Features.Authentication.DTOs;

namespace ChatApp.Mobile.Features.Authentication.Services;

public interface IAuthenticationStateService
{
    bool IsAuthenticated { get; }
    UserProfileDto? CurrentUser { get; }
    string? AccessToken { get; }
    event Action<bool>? AuthenticationStateChanged;

    void SetAuthenticated(UserProfileDto user, string accessToken, string refreshToken);
    void UpdateCurrentUser(UserProfileDto user);
    void SetUnauthenticated();
    Task InitializeAsync();
}
