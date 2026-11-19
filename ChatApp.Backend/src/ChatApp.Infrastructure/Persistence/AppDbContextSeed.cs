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
                CoverImageUrl = "https://api.iconify.design/noto:rocket.svg",
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            };

            var stickers = new List<Sticker>
            {
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:rocket.svg", OrderIndex = 0 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:smiling-face-with-sunglasses.svg", OrderIndex = 1 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:fire.svg", OrderIndex = 2 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:sparkles.svg", OrderIndex = 3 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:hot-beverage.svg", OrderIndex = 4 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:light-bulb.svg", OrderIndex = 5 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:party-popper.svg", OrderIndex = 6 },
                new() { Id = Guid.NewGuid(), StickerPackId = officialPack.Id, ImageUrl = "https://api.iconify.design/noto:handshake.svg", OrderIndex = 7 }
            };

            context.StickerPacks.Add(officialPack);
            context.Stickers.AddRange(stickers);
            await context.SaveChangesAsync();
        }
    }
}
