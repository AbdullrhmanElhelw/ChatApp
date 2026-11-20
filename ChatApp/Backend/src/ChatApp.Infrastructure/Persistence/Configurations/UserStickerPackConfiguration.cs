using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class UserStickerPackConfiguration : IEntityTypeConfiguration<UserStickerPack>
{
    public void Configure(EntityTypeBuilder<UserStickerPack> builder)
    {
        builder.HasKey(usp => new { usp.UserId, usp.StickerPackId });

        builder.HasOne(usp => usp.User)
            .WithMany(u => u.InstalledStickerPacks)
            .HasForeignKey(usp => usp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(usp => usp.StickerPack)
            .WithMany(sp => sp.InstalledByUsers)
            .HasForeignKey(usp => usp.StickerPackId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
