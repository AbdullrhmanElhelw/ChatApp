using ChatApp.Mobile.Features.Chat.DTOs;

namespace ChatApp.Mobile.Features.Chat.Services;

public interface IChatHubService
{
    bool IsConnected { get; }

    Task ConnectAsync();
    Task DisconnectAsync();
    Task JoinConversationAsync(Guid conversationId);
    Task LeaveConversationAsync(Guid conversationId);
    Task SendMessageAsync(SendMessageRequestClientDto request);
    Task MarkAsDeliveredAsync(Guid messageId);
    Task MarkAsReadAsync(Guid conversationId, Guid? messageId = null);
    Task SendTypingIndicatorAsync(Guid conversationId, bool isTyping);
    Task ReactToMessageAsync(Guid messageId, string emoji);

    event Action<MessageClientDto>? MessageReceived;
    event Action<MessageClientDto>? MessageConfirmationReceived;
    event Action<MessageStatusUpdateClientDto>? MessageStatusChanged;
    event Action<MessageReactionUpdateClientDto>? MessageReactionUpdated;
    event Action<TypingIndicatorClientDto>? UserTyping;
    event Action<Guid, bool, DateTime?>? UserPresenceChanged;
    event Action<bool>? ConnectionStateChanged;
}
