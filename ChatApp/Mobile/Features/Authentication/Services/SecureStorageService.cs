using System.Collections.Concurrent;
using Microsoft.Maui.Storage;

namespace ChatApp.Mobile.Features.Authentication.Services;

public class SecureStorageService : ISecureStorageService
{
    private const string AccessTokenKey = "chat_access_token";
    private const string RefreshTokenKey = "chat_refresh_token";
    private const string UserIdKey = "chat_user_id";
    private const string UserNameKey = "chat_user_name";
    private const string EmailKey = "chat_user_email";
    private const string FullNameKey = "chat_user_fullname";

    // In-memory cache + fallback. Reads hit this first so we don't decrypt from the Android Keystore
    // on every single API call / SignalR request. ConcurrentDictionary: many requests read tokens in parallel.
    private readonly ConcurrentDictionary<string, string> _memoryFallback = new();

    public async Task<string?> GetAccessTokenAsync() => await SafeGetAsync(AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync() => await SafeGetAsync(RefreshTokenKey);

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SafeSetAsync(AccessTokenKey, accessToken);
        await SafeSetAsync(RefreshTokenKey, refreshToken);
    }

    public async Task SaveUserDataAsync(Guid userId, string userName, string email, string fullName)
    {
        await SafeSetAsync(UserIdKey, userId.ToString());
        await SafeSetAsync(UserNameKey, userName);
        await SafeSetAsync(EmailKey, email);
        await SafeSetAsync(FullNameKey, fullName);
    }

    public async Task<string?> GetUserNameAsync() => await SafeGetAsync(UserNameKey);

    public async Task<string?> GetFullNameAsync() => await SafeGetAsync(FullNameKey);

    public async Task<Guid?> GetUserIdAsync()
    {
        var val = await SafeGetAsync(UserIdKey);
        return Guid.TryParse(val, out var guid) ? guid : null;
    }

    public async Task ClearAllAsync()
    {
        try
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(UserIdKey);
            SecureStorage.Default.Remove(UserNameKey);
            SecureStorage.Default.Remove(EmailKey);
            SecureStorage.Default.Remove(FullNameKey);
        }
        catch
        {
            // Ignore platform storage clearing errors
        }

        _memoryFallback.Clear();
        await Task.CompletedTask;
    }

    private async Task<string?> SafeGetAsync(string key)
    {
        if (_memoryFallback.TryGetValue(key, out var cached) && !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        try
        {
            var value = await SecureStorage.Default.GetAsync(key);
            if (!string.IsNullOrEmpty(value))
            {
                _memoryFallback[key] = value;
                return value;
            }
        }
        catch
        {
            // Platform storage read issue - nothing cached either
        }

        return null;
    }

    private async Task SafeSetAsync(string key, string value)
    {
        _memoryFallback[key] = value;
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch
        {
            // SecureStorage might not be supported in some headless test runners; memory fallback preserves state
        }
    }
}
