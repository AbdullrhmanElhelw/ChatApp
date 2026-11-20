namespace ChatApp.Application.Features.Stickers.DTOs;

public record StickerDto(
    Guid Id,
    Guid StickerPackId,
    string ImageUrl,
    int Width,
    int Height,
    int OrderIndex
);

public record StickerPackDto(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string Name,
    string? Description,
    string CoverImageUrl,
    bool IsPublic,
    int StickerCount,
    List<StickerDto> Stickers,
    DateTime CreatedAt
);

public record CreateStickerPackRequestDto(
    string Name,
    string? Description,
    string CoverImageUrl,
    bool IsPublic = true
);

public record AddStickerRequestDto(
    string ImageUrl,
    int Width = 512,
    int Height = 512
);
