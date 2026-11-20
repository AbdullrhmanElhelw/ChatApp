using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Chat.Services;

public interface IStickerApiService
{
    Task<ApiResult<List<StickerPackClientDto>>> GetPublicPacksAsync();
    Task<ApiResult<List<StickerPackClientDto>>> GetMyPacksAsync();
    Task<ApiResult<StickerPackClientDto>> CreatePackAsync(string name, string? description, string coverImageUrl);
    Task<ApiResult<StickerClientDto>> AddStickerToPackAsync(Guid packId, string imageUrl);
    Task<ApiResult<UploadMediaResponseClientDto>> UploadMediaAsync(Stream stream, string fileName, string contentType, MessageType mediaType);
}
