using ChatApp.Domain.Enums;

namespace ChatApp.Application.Features.Conversations.DTOs;

public record LastMessageDto(
    Guid Id,
    string? Content,
    MessageType MessageType,
    DateTime SentAt,
    Guid SenderId,
    string SenderName
);

public record ConversationSummaryDto(
    Guid Id,
    ConversationType ConversationType,
    string Title,
    string? AvatarUrl,
    Guid? OtherParticipantId,
    string? OtherParticipantUserName,
    bool IsOtherParticipantOnline,
    DateTime? OtherParticipantLastSeenAt,
    LastMessageDto? LastMessage,
    int UnreadCount,
    bool IsPinned,
    bool IsMuted,
    bool IsArchived,
    DateTime LastActivityAt
);

public record ConversationParticipantDto(
    Guid UserId,
    string UserName,
    string FullName,
    string? AvatarUrl,
    ParticipantRole Role,
    bool IsOnline,
    DateTime JoinedAt,
    DateTime? LastReadAt
);

public record ConversationDetailDto(
    Guid Id,
    ConversationType ConversationType,
    string Title,
    string? AvatarUrl,
    DateTime CreatedAt,
    List<ConversationParticipantDto> Participants
);

public record CreateDirectConversationRequestDto(
    Guid RecipientId
);
