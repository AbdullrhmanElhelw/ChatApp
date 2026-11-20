namespace ChatApp.Mobile.Features.Authentication.Services;

public interface ISecureStorageService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task SaveUserDataAsync(Guid userId, string userName, string email, string fullName);
    Task<string?> GetUserNameAsync();
    Task<string?> GetFullNameAsync();
    Task<Guid?> GetUserIdAsync();
    Task ClearAllAsync();
}
