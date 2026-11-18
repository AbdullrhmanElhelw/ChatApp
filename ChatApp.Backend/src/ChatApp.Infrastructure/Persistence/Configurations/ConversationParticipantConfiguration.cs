using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.HasKey(cp => new { cp.ConversationId, cp.UserId });

        builder.HasOne(cp => cp.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
            .WithMany(u => u.Participations)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.LastReadMessage)
            .WithMany()
            .HasForeignKey(cp => cp.LastReadMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cp => new { cp.UserId, cp.IsPinned, cp.JoinedAt });
    }
}
