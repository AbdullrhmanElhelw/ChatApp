using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Conversations.Services;

public interface IConversationApiService
{
    Task<ApiResult<List<ConversationSummaryDto>>> GetConversationsAsync();
    Task<ApiResult<ConversationDetailDto>> GetOrCreateDirectConversationAsync(Guid recipientId);
    Task<ApiResult<ConversationDetailDto>> GetConversationByIdAsync(Guid conversationId);
    Task<ApiResult<List<UserProfileDto>>> SearchUsersAsync(string? query, int pageIndex = 1, int pageSize = 20);
}
