using System.Text.Json.Serialization;

namespace ChatApp.Mobile.Features.CoupleSpace.DTOs;

public record CouplePartnerClientDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("userName")] string UserName,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl,
    [property: JsonPropertyName("isOnline")] bool IsOnline,
    [property: JsonPropertyName("lastSeenAt")] DateTime? LastSeenAt,
    [property: JsonPropertyName("mood")] string? Mood
);

public record CoupleSpaceClientDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("inviteCode")] string InviteCode,
    [property: JsonPropertyName("isPaired")] bool IsPaired,
    [property: JsonPropertyName("pairedAt")] DateTime? PairedAt,
    [property: JsonPropertyName("myMood")] string? MyMood,
    [property: JsonPropertyName("partner")] CouplePartnerClientDto? Partner,
    [property: JsonPropertyName("specialDatesJson")] string? SpecialDatesJson,
    [property: JsonPropertyName("memoriesJson")] string? MemoriesJson,
    [property: JsonPropertyName("loveNotesJson")] string? LoveNotesJson,
    [property: JsonPropertyName("dailyAnswersJson")] string? DailyAnswersJson
);

public record JoinCoupleRequestClientDto(
    [property: JsonPropertyName("inviteCode")] string InviteCode
);

public record UpdateMoodRequestClientDto(
    [property: JsonPropertyName("mood")] string Mood
);

public record SyncCoupleSpaceRequestClientDto(
    [property: JsonPropertyName("specialDatesJson")] string? SpecialDatesJson,
    [property: JsonPropertyName("memoriesJson")] string? MemoriesJson,
    [property: JsonPropertyName("loveNotesJson")] string? LoveNotesJson,
    [property: JsonPropertyName("dailyAnswersJson")] string? DailyAnswersJson
);
