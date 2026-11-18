using Microsoft.AspNetCore.Identity;

namespace ChatApp.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        Id = Guid.NewGuid();
    }

    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
    public ICollection<ConversationParticipant> Participations { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<MessageDelivery> MessageDeliveries { get; set; } = new List<MessageDelivery>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    public ICollection<StickerPack> CreatedStickerPacks { get; set; } = new List<StickerPack>();
    public ICollection<UserStickerPack> InstalledStickerPacks { get; set; } = new List<UserStickerPack>();
}
