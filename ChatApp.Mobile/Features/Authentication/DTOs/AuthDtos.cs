using System.Text.Json.Serialization;

namespace ChatApp.Mobile.Features.Authentication.DTOs;

public record RegisterRequestDto(
    string FullName,
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginRequestDto(
    string UserNameOrEmail,
    string Password
);

public record RefreshTokenRequestDto(
    string RefreshToken
);

public record AuthResponseDto(
    Guid UserId,
    string UserName,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record UserProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? Bio,
    string? AvatarUrl,
    bool IsOnline,
    DateTime? LastSeenAt,
    DateTime CreatedAt
);

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class ApiErrorResponse
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public string GetFormattedErrors()
    {
        if (Errors != null && Errors.Count > 0)
        {
            var errorList = new List<string>();
            foreach (var kvp in Errors)
            {
                errorList.AddRange(kvp.Value);
            }
            return string.Join("\n• ", errorList);
        }

        return Detail ?? Message ?? Title ?? "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";
    }
}
