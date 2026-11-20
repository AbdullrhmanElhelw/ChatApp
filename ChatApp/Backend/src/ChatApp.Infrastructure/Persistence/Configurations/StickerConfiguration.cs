using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class StickerConfiguration : IEntityTypeConfiguration<Sticker>
{
    public void Configure(EntityTypeBuilder<Sticker> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(s => s.StickerPack)
            .WithMany(sp => sp.Stickers)
            .HasForeignKey(s => s.StickerPackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
