using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Conversations.DTOs;

namespace ChatApp.Application.Features.Conversations.Interfaces;

public interface IConversationService
{
    Task<ApiResponse<List<ConversationSummaryDto>>> GetUserConversationsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ConversationDetailDto>> GetOrCreateDirectConversationAsync(
        Guid currentUserId,
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ConversationDetailDto>> GetConversationByIdAsync(
        Guid currentUserId,
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
