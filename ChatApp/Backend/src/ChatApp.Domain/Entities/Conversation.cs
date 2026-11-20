using ChatApp.Domain.Common;
using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class Conversation : BaseEntity, ISoftDeletable
{
    public ConversationType ConversationType { get; set; } = ConversationType.Direct;
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid CreatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigations
    public AppUser? CreatedBy { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
