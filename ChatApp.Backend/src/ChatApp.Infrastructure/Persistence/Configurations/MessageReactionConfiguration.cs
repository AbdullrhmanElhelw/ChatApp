using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.Emoji)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasOne(mr => mr.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mr => mr.User)
            .WithMany(u => u.Reactions)
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mr => new { mr.MessageId, mr.UserId })
            .IsUnique();
    }
}
