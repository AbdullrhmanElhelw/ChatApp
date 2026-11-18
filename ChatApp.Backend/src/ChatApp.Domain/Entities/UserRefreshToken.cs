using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class UserRefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public AppUser? User { get; set; }
}
