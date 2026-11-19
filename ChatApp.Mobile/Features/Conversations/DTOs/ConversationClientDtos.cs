using System.Text.Json.Serialization;

namespace ChatApp.Mobile.Features.Conversations.DTOs;

public enum ConversationType
{
    Direct = 1,
    Group = 2
}

public enum MessageType
{
    Text = 1,
    Image = 2,
    Video = 3,
    Audio = 4,
    Sticker = 5
}

public enum ParticipantRole
{
    Member = 1,
    Admin = 2
}

public record LastMessageDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("messageType")] MessageType MessageType,
    [property: JsonPropertyName("sentAt")] DateTime SentAt,
    [property: JsonPropertyName("senderId")] Guid SenderId,
    [property: JsonPropertyName("senderName")] string SenderName
);

public record ConversationSummaryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("conversationType")] ConversationType ConversationType,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl,
    [property: JsonPropertyName("otherParticipantId")] Guid? OtherParticipantId,
    [property: JsonPropertyName("otherParticipantUserName")] string? OtherParticipantUserName,
    [property: JsonPropertyName("isOtherParticipantOnline")] bool IsOtherParticipantOnline,
    [property: JsonPropertyName("otherParticipantLastSeenAt")] DateTime? OtherParticipantLastSeenAt,
    [property: JsonPropertyName("lastMessage")] LastMessageDto? LastMessage,
    [property: JsonPropertyName("unreadCount")] int UnreadCount,
    [property: JsonPropertyName("isPinned")] bool IsPinned,
    [property: JsonPropertyName("isMuted")] bool IsMuted,
    [property: JsonPropertyName("isArchived")] bool IsArchived,
    [property: JsonPropertyName("lastActivityAt")] DateTime LastActivityAt
)
{
    public string FormattedTime
    {
        get
        {
            var now = DateTime.UtcNow;
            var diff = now - LastActivityAt;

            if (diff.TotalMinutes < 1) return "الآن";
            if (diff.TotalMinutes < 60) return $"منذ {(int)diff.TotalMinutes} د";
            if (diff.TotalHours < 24 && LastActivityAt.Date == now.Date) return LastActivityAt.ToLocalTime().ToString("hh:mm tt");
            if (diff.TotalDays < 7) return LastActivityAt.ToLocalTime().ToString("dddd");
            return LastActivityAt.ToLocalTime().ToString("dd/MM/yyyy");
        }
    }

    public string LastMessagePreview
    {
        get
        {
            if (LastMessage is null) return "لا توجد رسائل بعد";
            if (!string.IsNullOrWhiteSpace(LastMessage.Content)) return LastMessage.Content;
            return LastMessage.MessageType switch
            {
                MessageType.Image => "📷 صورة",
                MessageType.Video => "🎥 فيديو",
                MessageType.Audio => "🎤 رسالة صوتية",
                MessageType.Sticker => "✨ ملصق",
                _ => "رسالة جديدة"
            };
        }
    }
}

public record ConversationParticipantDto(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("userName")] string UserName,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl,
    [property: JsonPropertyName("role")] ParticipantRole Role,
    [property: JsonPropertyName("isOnline")] bool IsOnline,
    [property: JsonPropertyName("joinedAt")] DateTime JoinedAt,
    [property: JsonPropertyName("lastReadAt")] DateTime? LastReadAt
);

public record ConversationDetailDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("conversationType")] ConversationType ConversationType,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("participants")] List<ConversationParticipantDto> Participants
);

public record CreateDirectConversationRequestDto(
    [property: JsonPropertyName("recipientId")] Guid RecipientId
);

public class PagedResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
