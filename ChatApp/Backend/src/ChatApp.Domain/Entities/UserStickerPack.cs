namespace ChatApp.Domain.Entities;

public class UserStickerPack
{
    public Guid UserId { get; set; }
    public Guid StickerPackId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int OrderIndex { get; set; }

    // Navigations
    public AppUser? User { get; set; }
    public StickerPack? StickerPack { get; set; }
}
