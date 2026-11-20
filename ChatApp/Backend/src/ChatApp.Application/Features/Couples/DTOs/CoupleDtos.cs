namespace ChatApp.Application.Features.Couples.DTOs;

public record CouplePartnerDto(
    Guid Id,
    string UserName,
    string FullName,
    string? AvatarUrl,
    bool IsOnline,
    DateTime? LastSeenAt,
    string? Mood
);

public record CoupleSpaceDto(
    Guid Id,
    string InviteCode,
    bool IsPaired,
    DateTime? PairedAt,
    string? MyMood,
    CouplePartnerDto? Partner,
    string? SpecialDatesJson,
    string? MemoriesJson,
    string? LoveNotesJson,
    string? DailyAnswersJson
);

public record JoinCoupleRequestDto(string InviteCode);

public record UpdateMoodRequestDto(string Mood);

public record SyncCoupleSpaceRequestDto(
    string? SpecialDatesJson,
    string? MemoriesJson,
    string? LoveNotesJson,
    string? DailyAnswersJson
);
