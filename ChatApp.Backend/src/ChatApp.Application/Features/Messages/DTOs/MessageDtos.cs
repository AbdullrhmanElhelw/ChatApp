using ChatApp.Domain.Enums;

namespace ChatApp.Application.Features.Messages.DTOs;

public record SendMessageRequestDto(
    Guid ConversationId,
    string? Content,
    MessageType MessageType = MessageType.Text,
    Guid ClientMessageId = default,
    Guid? ReplyToMessageId = null,
    Guid? MediaAttachmentId = null,
    Guid? StickerId = null
);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderUserName,
    string SenderFullName,
    string? SenderAvatarUrl,
    string? Content,
    MessageType MessageType,
    Guid ClientMessageId,
    DateTime SentAt,
    DateTime? EditedAt,
    Guid? ReplyToMessageId,
    string? ReplyToContent,
    DeliveryStatus DeliveryStatus,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    bool IsMine,
    string? MediaUrl = null,
    string? ThumbnailUrl = null,
    long? MediaSizeBytes = null,
    string? ReplyToSenderName = null
);

public record MessageStatusUpdateDto(
    Guid MessageId,
    Guid ConversationId,
    DeliveryStatus Status,
    DateTime Timestamp
);

public record TypingIndicatorDto(
    Guid ConversationId,
    Guid UserId,
    string UserName,
    bool IsTyping
);

public record MarkReadRequestDto(
    Guid? UpToMessageId
);
