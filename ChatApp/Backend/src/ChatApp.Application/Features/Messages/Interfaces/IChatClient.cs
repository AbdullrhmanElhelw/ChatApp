using ChatApp.Application.Features.Messages.DTOs;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Features.Messages.Interfaces;

public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);
    Task ReceiveMessageConfirmation(MessageDto message);
    Task MessageStatusChanged(MessageStatusUpdateDto statusUpdate);
    Task UserTyping(TypingIndicatorDto typing);
    Task UserPresenceChanged(Guid userId, bool isOnline, DateTime? lastSeenAt);
    Task MessageReactionUpdated(MessageReactionUpdateDto reactionUpdate);
    Task MessageEdited(MessageEditedDto edited);
    Task MessageDeleted(MessageDeletedDto deleted);
}
