using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Interfaces;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Conversations.DTOs;
using ChatApp.Application.Features.Conversations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[Authorize]
public class ConversationsController : ApiControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ICurrentUserService _currentUserService;

    public ConversationsController(
        IConversationService conversationService,
        ICurrentUserService currentUserService)
    {
        _conversationService = conversationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieves all active conversations for the authenticated user, ordered by pinned and recent activity.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ConversationSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ConversationSummaryDto>>>> GetConversations(
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _conversationService.GetUserConversationsAsync(currentUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets an existing direct conversation with the recipient or creates a new one idempotently.
    /// </summary>
    [HttpPost("direct")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> GetOrCreateDirect(
        [FromBody] CreateDirectConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _conversationService.GetOrCreateDirectConversationAsync(
            currentUserId,
            request.RecipientId,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed information and participant list for a specific conversation.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _conversationService.GetConversationByIdAsync(currentUserId, id, cancellationToken);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User identity could not be verified from token.");
        }
        return currentUserId.Value;
    }
}
