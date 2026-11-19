using System.Text.Json.Serialization;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Chat.DTOs;

public record StickerClientDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("stickerPackId")] Guid StickerPackId,
    [property: JsonPropertyName("imageUrl")] string ImageUrl,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("orderIndex")] int OrderIndex
);

public record StickerPackClientDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("ownerId")] Guid OwnerId,
    [property: JsonPropertyName("ownerName")] string OwnerName,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("coverImageUrl")] string CoverImageUrl,
    [property: JsonPropertyName("isPublic")] bool IsPublic,
    [property: JsonPropertyName("stickerCount")] int StickerCount,
    [property: JsonPropertyName("stickers")] List<StickerClientDto> Stickers,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt
);

public record CreateStickerPackClientDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("coverImageUrl")] string CoverImageUrl,
    [property: JsonPropertyName("isPublic")] bool IsPublic = true
);

public record AddStickerClientDto(
    [property: JsonPropertyName("imageUrl")] string ImageUrl,
    [property: JsonPropertyName("width")] int Width = 512,
    [property: JsonPropertyName("height")] int Height = 512
);

public record UploadMediaResponseClientDto(
    [property: JsonPropertyName("mediaId")] Guid MediaId,
    [property: JsonPropertyName("fileUrl")] string FileUrl,
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("fileSizeBytes")] long FileSizeBytes,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("durationSeconds")] int? DurationSeconds,
    [property: JsonPropertyName("mediaType")] MessageType MediaType,
    [property: JsonPropertyName("uploadedAt")] DateTime UploadedAt
);
