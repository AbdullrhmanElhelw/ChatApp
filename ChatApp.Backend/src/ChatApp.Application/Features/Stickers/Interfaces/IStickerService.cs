using ChatApp.Application.Features.Stickers.DTOs;

namespace ChatApp.Application.Features.Stickers.Interfaces;

public interface IStickerService
{
    Task<List<StickerPackDto>> GetPublicPacksAsync(CancellationToken cancellationToken = default);

    Task<List<StickerPackDto>> GetUserInstalledPacksAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<StickerPackDto?> GetPackByIdAsync(Guid packId, CancellationToken cancellationToken = default);

    Task<StickerPackDto> CreatePackAsync(
        Guid ownerId,
        CreateStickerPackRequestDto request,
        CancellationToken cancellationToken = default);

    Task<StickerDto> AddStickerToPackAsync(
        Guid ownerId,
        Guid packId,
        AddStickerRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> InstallPackAsync(Guid userId, Guid packId, CancellationToken cancellationToken = default);

    Task<bool> UninstallPackAsync(Guid userId, Guid packId, CancellationToken cancellationToken = default);
}
