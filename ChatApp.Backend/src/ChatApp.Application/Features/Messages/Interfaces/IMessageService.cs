using ChatApp.Application.Features.Messages.DTOs;

namespace ChatApp.Application.Features.Messages.Interfaces;

public interface IMessageService
{
    Task<List<MessageDto>> GetMessagesAsync(
        Guid currentUserId,
        Guid conversationId,
        DateTime? before = null,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<MessageDto> SendMessageAsync(
        Guid currentUserId,
        SendMessageRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MessageStatusUpdateDto?> MarkAsDeliveredAsync(
        Guid currentUserId,
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task<List<MessageStatusUpdateDto>> MarkConversationAsReadAsync(
        Guid currentUserId,
        Guid conversationId,
        Guid? upToMessageId = null,
        CancellationToken cancellationToken = default);

    Task<MessageReactionUpdateDto> ToggleReactionAsync(
        Guid currentUserId,
        Guid messageId,
        string emoji,
        CancellationToken cancellationToken = default);
}
