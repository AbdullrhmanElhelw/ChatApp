using System.Text.Json;
using ChatApp.Mobile.Features.Authentication.DTOs;

namespace ChatApp.Mobile.Features.Authentication.Services;

/// <summary>
/// Remembers the last known profile locally so the app can open straight to the chats list
/// (like WhatsApp) and verify the session with the server in the background.
/// </summary>
public static class CachedProfileStore
{
    private const string Key = "cached_user_profile_v1";

    public static void Save(UserProfileDto user)
    {
        try { Preferences.Default.Set(Key, JsonSerializer.Serialize(user)); }
        catch { /* best effort */ }
    }

    public static UserProfileDto? Load()
    {
        try
        {
            var json = Preferences.Default.Get(Key, string.Empty);
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserProfileDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        try { Preferences.Default.Remove(Key); }
        catch { /* best effort */ }
    }
}
