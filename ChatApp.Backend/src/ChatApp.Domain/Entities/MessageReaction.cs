using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;

    // Navigations
    public Message? Message { get; set; }
    public AppUser? User { get; set; }
}
