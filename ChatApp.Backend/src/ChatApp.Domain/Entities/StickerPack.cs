using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class StickerPack : BaseEntity, ISoftDeletable
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigations
    public AppUser? Owner { get; set; }
    public ICollection<Sticker> Stickers { get; set; } = new List<Sticker>();
    public ICollection<UserStickerPack> InstalledByUsers { get; set; } = new List<UserStickerPack>();
}
