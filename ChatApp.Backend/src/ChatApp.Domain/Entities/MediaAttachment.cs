using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class MediaAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Message? Message { get; set; }
}
