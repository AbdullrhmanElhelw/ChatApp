using ChatApp.Domain.Common;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageDelivery> MessageDeliveries => Set<MessageDelivery>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MediaAttachment> MediaAttachments => Set<MediaAttachment>();
    public DbSet<StickerPack> StickerPacks => Set<StickerPack>();
    public DbSet<Sticker> Stickers => Set<Sticker>();
    public DbSet<UserStickerPack> UserStickerPacks => Set<UserStickerPack>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from current assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filters for soft-deletable entities and their required dependents
        builder.Entity<Conversation>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<ConversationParticipant>().HasQueryFilter(cp => !cp.Conversation!.IsDeleted);
        builder.Entity<Message>().HasQueryFilter(m => !m.IsDeleted);
        builder.Entity<MessageDelivery>().HasQueryFilter(md => !md.Message!.IsDeleted);
        builder.Entity<MessageReaction>().HasQueryFilter(mr => !mr.Message!.IsDeleted);
        builder.Entity<StickerPack>().HasQueryFilter(sp => !sp.IsDeleted);
        builder.Entity<Sticker>().HasQueryFilter(s => !s.StickerPack!.IsDeleted);
        builder.Entity<UserStickerPack>().HasQueryFilter(usp => !usp.StickerPack!.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
