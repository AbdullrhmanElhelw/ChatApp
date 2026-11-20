using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Features.Stickers.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatApp.UnitTests.Services;

public class StickerServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreatePack_WhenValid_ShouldPersistPackAndAutoInstallForOwner()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new StickerService(db);
        var ownerId = Guid.NewGuid();

        var owner = new AppUser { Id = ownerId, UserName = "owner", FullName = "Owner User" };
        db.Users.Add(owner);
        await db.SaveChangesAsync();

        var req = new CreateStickerPackRequestDto("Memes 2026", "Funny memes", "/uploads/stickers/cover.webp", true);

        // Act
        var result = await sut.CreatePackAsync(ownerId, req);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Memes 2026");
        result.OwnerId.Should().Be(ownerId);

        var packInDb = await db.StickerPacks.FirstOrDefaultAsync(sp => sp.Id == result.Id);
        packInDb.Should().NotBeNull();

        var installed = await db.UserStickerPacks.FirstOrDefaultAsync(u => u.UserId == ownerId && u.StickerPackId == result.Id);
        installed.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePack_WhenNameEmpty_ShouldThrowValidationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new StickerService(db);

        var req = new CreateStickerPackRequestDto("", null, "/cover.png");

        // Act & Assert
        var act = () => sut.CreatePackAsync(Guid.NewGuid(), req);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddStickerToPack_WhenOwner_ShouldAddSticker()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new StickerService(db);
        var ownerId = Guid.NewGuid();
        var packId = Guid.NewGuid();

        var pack = new StickerPack
        {
            Id = packId,
            OwnerId = ownerId,
            Name = "My Pack",
            CoverImageUrl = "/cover.webp",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow
        };

        db.StickerPacks.Add(pack);
        await db.SaveChangesAsync();

        var req = new AddStickerRequestDto("/uploads/stickers/stk1.webp", 512, 512);

        // Act
        var result = await sut.AddStickerToPackAsync(ownerId, packId, req);

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be("/uploads/stickers/stk1.webp");
        result.StickerPackId.Should().Be(packId);

        var count = await db.Stickers.CountAsync(s => s.StickerPackId == packId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddStickerToPack_WhenNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new StickerService(db);
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var packId = Guid.NewGuid();

        var pack = new StickerPack
        {
            Id = packId,
            OwnerId = ownerId,
            Name = "My Pack",
            CoverImageUrl = "/cover.webp",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow
        };

        db.StickerPacks.Add(pack);
        await db.SaveChangesAsync();

        var req = new AddStickerRequestDto("/uploads/stickers/stk1.webp");

        // Act & Assert
        var act = () => sut.AddStickerToPackAsync(otherUserId, packId, req);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task InstallPack_WhenNew_ShouldAddUserStickerPack()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new StickerService(db);
        var userId = Guid.NewGuid();
        var packId = Guid.NewGuid();

        var pack = new StickerPack
        {
            Id = packId,
            OwnerId = Guid.NewGuid(),
            Name = "Cool Pack",
            CoverImageUrl = "/cover.webp",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow
        };

        db.StickerPacks.Add(pack);
        await db.SaveChangesAsync();

        // Act
        var result = await sut.InstallPackAsync(userId, packId);

        // Assert
        result.Should().BeTrue();
        var installed = await db.UserStickerPacks.AnyAsync(usp => usp.UserId == userId && usp.StickerPackId == packId);
        installed.Should().BeTrue();
    }

    [Fact]
    public async Task UninstallPack_WhenInstalled_ShouldRemoveUserStickerPack()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new StickerService(db);
        var userId = Guid.NewGuid();
        var packId = Guid.NewGuid();

        db.UserStickerPacks.Add(new UserStickerPack
        {
            UserId = userId,
            StickerPackId = packId,
            AddedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var result = await sut.UninstallPackAsync(userId, packId);

        // Assert
        result.Should().BeTrue();
        var exists = await db.UserStickerPacks.AnyAsync(usp => usp.UserId == userId && usp.StickerPackId == packId);
        exists.Should().BeFalse();
    }
}
