using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Chat.DTOs;

public enum MessageDeliveryStatus
{
    Sent = 1,
    Delivered = 2,
    Read = 3
}

public record SendMessageRequestClientDto(
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("messageType")] MessageType MessageType = MessageType.Text,
    [property: JsonPropertyName("clientMessageId")] Guid ClientMessageId = default,
    [property: JsonPropertyName("replyToMessageId")] Guid? ReplyToMessageId = null,
    [property: JsonPropertyName("mediaAttachmentId")] Guid? MediaAttachmentId = null,
    [property: JsonPropertyName("stickerId")] Guid? StickerId = null
);

public record MessageClientDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("senderId")] Guid SenderId,
    [property: JsonPropertyName("senderUserName")] string SenderUserName,
    [property: JsonPropertyName("senderFullName")] string SenderFullName,
    [property: JsonPropertyName("senderAvatarUrl")] string? SenderAvatarUrl,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("messageType")] MessageType MessageType,
    [property: JsonPropertyName("clientMessageId")] Guid ClientMessageId,
    [property: JsonPropertyName("sentAt")] DateTime SentAt,
    [property: JsonPropertyName("editedAt")] DateTime? EditedAt,
    [property: JsonPropertyName("replyToMessageId")] Guid? ReplyToMessageId,
    [property: JsonPropertyName("replyToContent")] string? ReplyToContent,
    [property: JsonPropertyName("deliveryStatus")] MessageDeliveryStatus DeliveryStatus,
    [property: JsonPropertyName("deliveredAt")] DateTime? DeliveredAt,
    [property: JsonPropertyName("readAt")] DateTime? ReadAt,
    [property: JsonPropertyName("isMine")] bool IsMine,
    [property: JsonPropertyName("mediaUrl")] string? MediaUrl = null,
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl = null,
    [property: JsonPropertyName("mediaSizeBytes")] long? MediaSizeBytes = null
);

public record MessageStatusUpdateClientDto(
    [property: JsonPropertyName("messageId")] Guid MessageId,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("status")] MessageDeliveryStatus Status,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record TypingIndicatorClientDto(
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("userName")] string UserName,
    [property: JsonPropertyName("isTyping")] bool IsTyping
);

public partial class ChatMessageItem : ObservableObject
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string SenderFullName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string? Content { get; set; }
    public MessageType MessageType { get; set; }
    public Guid ClientMessageId { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public string? ReplyToContent { get; set; }
    public bool IsMine { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? MediaSizeBytes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    [NotifyPropertyChangedFor(nameof(IsRead))]
    public partial MessageDeliveryStatus Status { get; set; } = MessageDeliveryStatus.Sent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    public partial bool IsSending { get; set; }

    public bool IsRead => Status == MessageDeliveryStatus.Read;

    public Color StatusColor => IsRead ? Color.FromArgb("#38BDF8") : Color.FromArgb("#E0F2FE");

    public string FormattedTime => SentAt.ToLocalTime().ToString("hh:mm tt");

    public bool HasMedia => !string.IsNullOrEmpty(MediaUrl);
    public bool IsImage => MessageType == MessageType.Image && HasMedia;
    public bool IsSticker => MessageType == MessageType.Sticker;

    public bool IsOutgoingNormal => IsMine && !IsSticker;
    public bool IsOutgoingSticker => IsMine && IsSticker;
    public bool IsIncomingNormal => !IsMine && !IsSticker;
    public bool IsIncomingSticker => !IsMine && IsSticker;

    public string StickerUrl
    {
        get
        {
            if (MessageType != MessageType.Sticker) return string.Empty;
            var url = MediaUrl ?? Content ?? "";
            if (string.IsNullOrEmpty(url)) return "";
            if (url.StartsWith("/")) return $"{ApiConfig.BaseUrl}{url}";
            return url;
        }
    }

    public string DisplayImageUrl
    {
        get
        {
            var raw = MessageType == MessageType.Sticker ? StickerUrl : (MediaUrl ?? "");
            if (string.IsNullOrEmpty(raw)) return "";
            if (raw.StartsWith("/")) return $"{ApiConfig.BaseUrl}{raw}";
            return raw;
        }
    }

    public bool IsTextMessage => MessageType == MessageType.Text && !IsSticker && !IsImage;

    public string StatusIcon
    {
        get
        {
            if (!IsMine) return string.Empty;
            if (IsSending) return "🕒";
            return Status switch
            {
                MessageDeliveryStatus.Sent => "✓",
                MessageDeliveryStatus.Delivered => "✓✓",
                MessageDeliveryStatus.Read => "✓✓",
                _ => "✓"
            };
        }
    }

    public static ChatMessageItem FromDto(MessageClientDto dto)
    {
        return new ChatMessageItem
        {
            Id = dto.Id,
            ConversationId = dto.ConversationId,
            SenderId = dto.SenderId,
            SenderUserName = dto.SenderUserName,
            SenderFullName = dto.SenderFullName,
            SenderAvatarUrl = dto.SenderAvatarUrl,
            Content = dto.Content,
            MessageType = dto.MessageType,
            ClientMessageId = dto.ClientMessageId,
            SentAt = dto.SentAt,
            EditedAt = dto.EditedAt,
            ReplyToMessageId = dto.ReplyToMessageId,
            ReplyToContent = dto.ReplyToContent,
            IsMine = dto.IsMine,
            Status = dto.DeliveryStatus,
            IsSending = false,
            MediaUrl = dto.MediaUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            MediaSizeBytes = dto.MediaSizeBytes
        };
    }
}
