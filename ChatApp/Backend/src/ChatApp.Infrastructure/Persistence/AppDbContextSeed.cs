using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!await context.StickerPacks.AnyAsync())
        {
            var adminUser = await context.Users.FirstOrDefaultAsync();
            var ownerId = adminUser?.Id ?? Guid.NewGuid();

            var officialPack = new StickerPack
            {
                Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                OwnerId = ownerId,
                Name = "ملصقات شات آب الرسمية ✨",
                Description = "حزمة الملصقات التعبيرية الرسمية لتطبيق ChatApp",
                CoverImageUrl = "/uploads/stickers/rocket.png",
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            };

            var stickers = new List<Sticker>
            {
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/rocket.png", OrderIndex = 0 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/cool.png", OrderIndex = 1 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/fire.png", OrderIndex = 2 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/sparkles.png", OrderIndex = 3 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/coffee.png", OrderIndex = 4 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/idea.png", OrderIndex = 5 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/party.png", OrderIndex = 6 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "/uploads/stickers/heart.png", OrderIndex = 7 }
            };

            context.StickerPacks.Add(officialPack);
            context.Stickers.AddRange(stickers);
            await context.SaveChangesAsync();
        }
    }
}
