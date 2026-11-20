using ChatApp.Domain.Common;
using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class Message : BaseEntity, ISoftDeletable
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string? Content { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public Guid ClientMessageId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public bool IsDeleted { get; set; }

    // Navigations
    public Conversation? Conversation { get; set; }
    public AppUser? Sender { get; set; }
    public Message? ReplyToMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public ICollection<MessageDelivery> Deliveries { get; set; } = new List<MessageDelivery>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    public ICollection<MediaAttachment> MediaAttachments { get; set; } = new List<MediaAttachment>();
}
