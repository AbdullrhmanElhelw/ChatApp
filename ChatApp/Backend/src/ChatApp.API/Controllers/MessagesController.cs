using System.Security.Claims;
using ChatApp.API.Hubs;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Messages.DTOs;
using ChatApp.Application.Features.Messages.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/conversations/{conversationId:guid}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public MessagesController(
        IMessageService messageService,
        IHubContext<ChatHub, IChatClient> hubContext)
    {
        _messageService = messageService;
        _hubContext = hubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(
        [FromRoute] Guid conversationId,
        [FromQuery] DateTime? before = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var messages = await _messageService.GetMessagesAsync(
            currentUserId,
            conversationId,
            before,
            pageSize,
            cancellationToken);

        return Ok(ApiResponse<List<MessageDto>>.Ok(messages));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendMessage(
        [FromRoute] Guid conversationId,
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var messageRequest = request with { ConversationId = conversationId };
        var message = await _messageService.SendMessageAsync(currentUserId, messageRequest, cancellationToken);

        // Broadcast to SignalR active room
        var groupName = ChatHub.GetConversationGroupName(conversationId);
        await _hubContext.Clients.Group(groupName).ReceiveMessage(message);

        return CreatedAtAction(
            nameof(GetMessages),
            new { conversationId },
            ApiResponse<MessageDto>.Ok(message));
    }

    [HttpPost("read")]
    [ProducesResponseType(typeof(ApiResponse<List<MessageStatusUpdateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] Guid conversationId,
        [FromBody] MarkReadRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var updates = await _messageService.MarkConversationAsReadAsync(
            currentUserId,
            conversationId,
            request?.UpToMessageId,
            cancellationToken);

        var groupName = ChatHub.GetConversationGroupName(conversationId);
        foreach (var update in updates)
        {
            await _hubContext.Clients.Group(groupName).MessageStatusChanged(update);
        }

        return Ok(ApiResponse<List<MessageStatusUpdateDto>>.Ok(updates));
    }

    [HttpPost("{messageId:guid}/reactions")]
    [ProducesResponseType(typeof(ApiResponse<MessageReactionUpdateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleReaction(
        [FromRoute] Guid conversationId,
        [FromRoute] Guid messageId,
        [FromBody] ReactRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var update = await _messageService.ToggleReactionAsync(currentUserId, messageId, request.Emoji, cancellationToken);

        var groupName = ChatHub.GetConversationGroupName(conversationId);
        await _hubContext.Clients.Group(groupName).MessageReactionUpdated(update);

        return Ok(ApiResponse<MessageReactionUpdateDto>.Ok(update));
    }

    [HttpPut("{messageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MessageEditedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditMessage(
        [FromRoute] Guid conversationId,
        [FromRoute] Guid messageId,
        [FromBody] EditMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var edited = await _messageService.EditMessageAsync(currentUserId, conversationId, messageId, request.Content, cancellationToken);

        var groupName = ChatHub.GetConversationGroupName(conversationId);
        await _hubContext.Clients.Group(groupName).MessageEdited(edited);

        return Ok(ApiResponse<MessageEditedDto>.Ok(edited));
    }

    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MessageDeletedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMessage(
        [FromRoute] Guid conversationId,
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var deleted = await _messageService.DeleteMessageAsync(currentUserId, conversationId, messageId, cancellationToken);

        var groupName = ChatHub.GetConversationGroupName(conversationId);
        await _hubContext.Clients.Group(groupName).MessageDeleted(deleted);

        return Ok(ApiResponse<MessageDeletedDto>.Ok(deleted));
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }
}
