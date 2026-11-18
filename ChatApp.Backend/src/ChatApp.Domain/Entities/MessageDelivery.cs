using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class MessageDelivery
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Sent;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigations
    public Message? Message { get; set; }
    public AppUser? User { get; set; }
}
