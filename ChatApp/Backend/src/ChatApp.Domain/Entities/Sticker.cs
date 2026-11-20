using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class Sticker : BaseEntity
{
    public Guid StickerPackId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int OrderIndex { get; set; }

    // Navigation
    public StickerPack? StickerPack { get; set; }
}
