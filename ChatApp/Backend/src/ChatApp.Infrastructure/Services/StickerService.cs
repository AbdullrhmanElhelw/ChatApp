using Microsoft.EntityFrameworkCore;
using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Features.Stickers.DTOs;
using ChatApp.Application.Features.Stickers.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;

namespace ChatApp.Infrastructure.Services;

public class StickerService : IStickerService
{
    private readonly AppDbContext _context;

    public StickerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<StickerPackDto>> GetPublicPacksAsync(CancellationToken cancellationToken = default)
    {
        var packs = await _context.StickerPacks
            .AsNoTracking()
            .Where(sp => sp.IsPublic && !sp.IsDeleted)
            .Include(sp => sp.Owner)
            .Include(sp => sp.Stickers)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(cancellationToken);

        return packs.Select(MapToDto).ToList();
    }

    public async Task<List<StickerPackDto>> GetUserInstalledPacksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Get packs installed via UserStickerPacks
        var installedPackIds = await _context.UserStickerPacks
            .AsNoTracking()
            .Where(usp => usp.UserId == userId)
            .Select(usp => usp.StickerPackId)
            .ToListAsync(cancellationToken);

        // 2. Fetch both installed packs and packs owned by this user
        var packs = await _context.StickerPacks
            .AsNoTracking()
            .Where(sp => !sp.IsDeleted && (installedPackIds.Contains(sp.Id) || sp.OwnerId == userId))
            .Include(sp => sp.Owner)
            .Include(sp => sp.Stickers)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(cancellationToken);

        return packs.Select(MapToDto).ToList();
    }

    public async Task<StickerPackDto?> GetPackByIdAsync(Guid packId, CancellationToken cancellationToken = default)
    {
        var pack = await _context.StickerPacks
            .AsNoTracking()
            .Include(sp => sp.Owner)
            .Include(sp => sp.Stickers)
            .FirstOrDefaultAsync(sp => sp.Id == packId && !sp.IsDeleted, cancellationToken);

        return pack != null ? MapToDto(pack) : null;
    }

    public async Task<StickerPackDto> CreatePackAsync(
        Guid ownerId,
        CreateStickerPackRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("اسم حزمة الاستيكرات مطلوب.");
        }

        var pack = new StickerPack
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CoverImageUrl = request.CoverImageUrl,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow
        };

        _context.StickerPacks.Add(pack);

        // Auto-install for creator
        var userPack = new UserStickerPack
        {
            UserId = ownerId,
            StickerPackId = pack.Id,
            AddedAt = DateTime.UtcNow,
            OrderIndex = 0
        };
        _context.UserStickerPacks.Add(userPack);

        await _context.SaveChangesAsync(cancellationToken);

        var owner = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);

        return new StickerPackDto(
            Id: pack.Id,
            OwnerId: ownerId,
            OwnerName: owner?.FullName ?? "مستخدم",
            Name: pack.Name,
            Description: pack.Description,
            CoverImageUrl: pack.CoverImageUrl,
            IsPublic: pack.IsPublic,
            StickerCount: 0,
            Stickers: new List<StickerDto>(),
            CreatedAt: pack.CreatedAt
        );
    }

    public async Task<StickerDto> AddStickerToPackAsync(
        Guid ownerId,
        Guid packId,
        AddStickerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var pack = await _context.StickerPacks
            .Include(sp => sp.Stickers)
            .FirstOrDefaultAsync(sp => sp.Id == packId && !sp.IsDeleted, cancellationToken);

        if (pack == null)
        {
            throw new NotFoundException("حزمة الاستيكرات غير موجودة.");
        }

        if (pack.OwnerId != ownerId)
        {
            throw new ForbiddenAccessException("ليس لديك صلاحية لإضافة استيكرات لهذه الحزمة.");
        }

        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            throw new ValidationException("رابط صورة الاستيكر مطلوب.");
        }

        var sticker = new Sticker
        {
            Id = Guid.NewGuid(),
            StickerPackId = packId,
            ImageUrl = request.ImageUrl,
            Width = request.Width > 0 ? request.Width : 512,
            Height = request.Height > 0 ? request.Height : 512,
            OrderIndex = pack.Stickers.Count,
            CreatedAt = DateTime.UtcNow
        };

        _context.Stickers.Add(sticker);
        await _context.SaveChangesAsync(cancellationToken);

        return new StickerDto(
            Id: sticker.Id,
            StickerPackId: sticker.StickerPackId,
            ImageUrl: sticker.ImageUrl,
            Width: sticker.Width,
            Height: sticker.Height,
            OrderIndex: sticker.OrderIndex
        );
    }

    public async Task<bool> InstallPackAsync(Guid userId, Guid packId, CancellationToken cancellationToken = default)
    {
        var pack = await _context.StickerPacks
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == packId && !sp.IsDeleted, cancellationToken);

        if (pack == null)
        {
            throw new NotFoundException("حزمة الاستيكرات غير موجودة.");
        }

        var exists = await _context.UserStickerPacks
            .AnyAsync(usp => usp.UserId == userId && usp.StickerPackId == packId, cancellationToken);

        if (exists) return true;

        var userPack = new UserStickerPack
        {
            UserId = userId,
            StickerPackId = packId,
            AddedAt = DateTime.UtcNow,
            OrderIndex = 0
        };

        _context.UserStickerPacks.Add(userPack);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UninstallPackAsync(Guid userId, Guid packId, CancellationToken cancellationToken = default)
    {
        var userPack = await _context.UserStickerPacks
            .FirstOrDefaultAsync(usp => usp.UserId == userId && usp.StickerPackId == packId, cancellationToken);

        if (userPack != null)
        {
            _context.UserStickerPacks.Remove(userPack);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static StickerPackDto MapToDto(StickerPack pack)
    {
        return new StickerPackDto(
            Id: pack.Id,
            OwnerId: pack.OwnerId,
            OwnerName: pack.Owner?.FullName ?? "مستخدم",
            Name: pack.Name,
            Description: pack.Description,
            CoverImageUrl: pack.CoverImageUrl,
            IsPublic: pack.IsPublic,
            StickerCount: pack.Stickers?.Count ?? 0,
            Stickers: pack.Stickers?
                .OrderBy(s => s.OrderIndex)
                .Select(s => new StickerDto(
                    Id: s.Id,
                    StickerPackId: s.StickerPackId,
                    ImageUrl: s.ImageUrl,
                    Width: s.Width,
                    Height: s.Height,
                    OrderIndex: s.OrderIndex
                )).ToList() ?? new List<StickerDto>(),
            CreatedAt: pack.CreatedAt
        );
    }
}
