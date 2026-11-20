using ChatApp.Application.Features.Media.DTOs;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Features.Media.Interfaces;

public interface IMediaService
{
    Task<UploadMediaResponseDto> UploadMediaAsync(
        Stream stream,
        string fileName,
        string contentType,
        MessageType mediaType,
        CancellationToken cancellationToken = default);

    Task<MediaAttachmentDto?> GetMediaByIdAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
