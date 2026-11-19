using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;

namespace ChatApp.Mobile.Features.Chat.Services;

public interface IMessageApiService
{
    Task<ApiResult<List<MessageClientDto>>> GetMessagesAsync(
        Guid conversationId,
        DateTime? before = null,
        int pageSize = 50);
}
