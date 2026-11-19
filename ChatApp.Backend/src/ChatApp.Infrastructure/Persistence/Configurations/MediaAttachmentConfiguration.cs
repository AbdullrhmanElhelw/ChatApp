using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class MediaAttachmentConfiguration : IEntityTypeConfiguration<MediaAttachment>
{
    public void Configure(EntityTypeBuilder<MediaAttachment> builder)
    {
        builder.HasKey(ma => ma.Id);

        builder.Property(ma => ma.FileUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ma => ma.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(ma => ma.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(ma => ma.Message)
            .WithMany(m => m.MediaAttachments)
            .HasForeignKey(ma => ma.MessageId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
