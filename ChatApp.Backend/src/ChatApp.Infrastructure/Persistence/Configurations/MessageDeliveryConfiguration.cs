using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class MessageDeliveryConfiguration : IEntityTypeConfiguration<MessageDelivery>
{
    public void Configure(EntityTypeBuilder<MessageDelivery> builder)
    {
        builder.HasKey(md => new { md.MessageId, md.UserId });

        builder.HasOne(md => md.Message)
            .WithMany(m => m.Deliveries)
            .HasForeignKey(md => md.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(md => md.User)
            .WithMany(u => u.MessageDeliveries)
            .HasForeignKey(md => md.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(md => new { md.UserId, md.Status });
    }
}
