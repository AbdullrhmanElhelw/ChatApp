using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantRole Role { get; set; } = ParticipantRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public Guid? LastReadMessageId { get; set; }
    public DateTime? LastReadAt { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }

    // Navigations
    public Conversation? Conversation { get; set; }
    public AppUser? User { get; set; }
    public Message? LastReadMessage { get; set; }
}
