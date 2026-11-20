using System.Security.Claims;
using ChatApp.Application.Features.Messages.DTOs;
using ChatApp.Application.Features.Messages.Interfaces;
using ChatApp.Application.Features.Users.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageService messageService,
        IUserService userService,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _userService = userService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            // Join user-specific personal group for cross-conversation alerts (new chats, inbox updates)
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

            // Update presence
            await _userService.UpdatePresenceAsync(userId, true);
            await Clients.Others.UserPresenceChanged(userId, true, null);

            _logger.LogInformation("User {UserId} connected to ChatHub (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

            var now = DateTime.UtcNow;
            await _userService.UpdatePresenceAsync(userId, false);
            await Clients.Others.UserPresenceChanged(userId, false, now);

            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var groupName = GetConversationGroupName(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("User {UserId} joined conversation group {ConversationId}", GetCurrentUserId(), conversationId);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        var groupName = GetConversationGroupName(conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("User {UserId} left conversation group {ConversationId}", GetCurrentUserId(), conversationId);
    }

    public async Task SendMessage(SendMessageRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return;

        try
        {
            var message = await _messageService.SendMessageAsync(userId, request);

            // Confirm back to caller
            await Clients.Caller.ReceiveMessageConfirmation(message);

            // Broadcast to all other participants actively in the conversation group
            var groupName = GetConversationGroupName(request.ConversationId);
            await Clients.OthersInGroup(groupName).ReceiveMessage(message);

            _logger.LogInformation("Message {MessageId} dispatched in conversation {ConversationId} by user {UserId}",
                message.Id, request.ConversationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message in conversation {ConversationId} for user {UserId}",
                request.ConversationId, userId);
            throw;
        }
    }

    public async Task MarkAsDelivered(Guid messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return;

        var update = await _messageService.MarkAsDeliveredAsync(userId, messageId);
        if (update != null)
        {
            var groupName = GetConversationGroupName(update.ConversationId);
            await Clients.Group(groupName).MessageStatusChanged(update);
        }
    }

    public async Task MarkAsRead(Guid conversationId, Guid? messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return;

        var updates = await _messageService.MarkConversationAsReadAsync(userId, conversationId, messageId);
        var groupName = GetConversationGroupName(conversationId);

        foreach (var update in updates)
        {
            await Clients.Group(groupName).MessageStatusChanged(update);
        }
    }

    public async Task SendTypingIndicator(Guid conversationId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return;

        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ??
                       Context.User?.FindFirstValue("username") ??
                       "Someone";

        var groupName = GetConversationGroupName(conversationId);
        await Clients.OthersInGroup(groupName).UserTyping(new TypingIndicatorDto(
            conversationId,
            userId,
            userName,
            isTyping
        ));
    }

    public async Task ReactToMessage(Guid messageId, string emoji)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return;

        try
        {
            var update = await _messageService.ToggleReactionAsync(userId, messageId, emoji);
            var groupName = GetConversationGroupName(update.ConversationId);
            await Clients.Group(groupName).MessageReactionUpdated(update);

            _logger.LogInformation("Reaction {Emoji} toggled on message {MessageId} by user {UserId}",
                emoji, messageId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to react to message {MessageId} for user {UserId}", messageId, userId);
            throw;
        }
    }

    private Guid GetCurrentUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  Context.User?.FindFirstValue("sub");

        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }

    public static string GetConversationGroupName(Guid conversationId) => $"conversation:{conversationId}";
    public static string GetUserGroupName(Guid userId) => $"user:{userId}";
}
