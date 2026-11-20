using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class StickerPackConfiguration : IEntityTypeConfiguration<StickerPack>
{
    public void Configure(EntityTypeBuilder<StickerPack> builder)
    {
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sp => sp.Description)
            .HasMaxLength(250);

        builder.Property(sp => sp.CoverImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(sp => sp.Owner)
            .WithMany(u => u.CreatedStickerPacks)
            .HasForeignKey(sp => sp.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sp => sp.Stickers)
            .WithOne(s => s.StickerPack)
            .HasForeignKey(s => s.StickerPackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sp => sp.InstalledByUsers)
            .WithOne(u => u.StickerPack)
            .HasForeignKey(u => u.StickerPackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
