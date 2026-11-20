using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Bio)
            .HasMaxLength(250);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.HasIndex(u => u.UserName);
        builder.HasIndex(u => u.Email);
    }
}
