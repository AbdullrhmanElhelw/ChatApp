using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Features.Messages.DTOs;
using ChatApp.Application.Features.Messages.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _dbContext;

    public MessageService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MessageDto>> GetMessagesAsync(
        Guid currentUserId,
        Guid conversationId,
        DateTime? before = null,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var isParticipant = await _dbContext.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId, cancellationToken);

        if (!isParticipant)
        {
            throw new ForbiddenAccessException("You are not a participant in this conversation.");
        }

        var query = _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.Deliveries)
            .Include(m => m.MediaAttachments)
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted);

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Reverse to return in chronological order (oldest to newest)
        messages.Reverse();

        return messages.Select(m => MapToDto(m, currentUserId)).ToList();
    }

    public async Task<MessageDto> SendMessageAsync(
        Guid currentUserId,
        SendMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var participants = await _dbContext.ConversationParticipants
            .Where(cp => cp.ConversationId == request.ConversationId)
            .ToListAsync(cancellationToken);

        var currentParticipant = participants.FirstOrDefault(cp => cp.UserId == currentUserId);
        if (currentParticipant == null)
        {
            throw new ForbiddenAccessException("You are not a participant in this conversation.");
        }

        // Idempotency check: if client supplied ClientMessageId, verify if already created
        if (request.ClientMessageId != Guid.Empty)
        {
            var existingMessage = await _dbContext.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.Deliveries)
                .Include(m => m.MediaAttachments)
                .FirstOrDefaultAsync(m => m.ClientMessageId == request.ClientMessageId, cancellationToken);

            if (existingMessage != null)
            {
                return MapToDto(existingMessage, currentUserId);
            }
        }

        var clientMessageId = request.ClientMessageId != Guid.Empty ? request.ClientMessageId : Guid.NewGuid();
        var now = DateTime.UtcNow;

        var msgType = request.MessageType;
        var content = request.Content;

        if (request.StickerId.HasValue)
        {
            var sticker = await _dbContext.Stickers.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.StickerId.Value, cancellationToken);
            if (sticker != null)
            {
                msgType = MessageType.Sticker;
                content = sticker.ImageUrl;
            }
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = currentUserId,
            Content = content,
            MessageType = msgType,
            ClientMessageId = clientMessageId,
            SentAt = now,
            ReplyToMessageId = request.ReplyToMessageId,
            IsDeleted = false
        };

        if (request.MediaAttachmentId.HasValue)
        {
            var attachment = await _dbContext.MediaAttachments
                .FirstOrDefaultAsync(ma => ma.Id == request.MediaAttachmentId.Value, cancellationToken);
            if (attachment != null)
            {
                attachment.MessageId = message.Id;
                message.MediaAttachments.Add(attachment);
                if (msgType == MessageType.Text)
                {
                    msgType = attachment.ContentType.StartsWith("image/") ? MessageType.Image
                        : attachment.ContentType.StartsWith("video/") ? MessageType.Video
                        : attachment.ContentType.StartsWith("audio/") ? MessageType.Audio
                        : MessageType.Image;
                    message.MessageType = msgType;
                }
            }
        }

        // Create message deliveries for all other conversation participants
        foreach (var p in participants.Where(cp => cp.UserId != currentUserId))
        {
            message.Deliveries.Add(new MessageDelivery
            {
                MessageId = message.Id,
                UserId = p.UserId,
                Status = DeliveryStatus.Sent,
                DeliveredAt = null,
                ReadAt = null
            });
        }

        // Update conversation last activity
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.UpdatedAt = now;
        }

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Load navigations for complete DTO
        var sender = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        message.Sender = sender;

        if (request.ReplyToMessageId.HasValue)
        {
            message.ReplyToMessage = await _dbContext.Messages.AsNoTracking()
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == request.ReplyToMessageId.Value, cancellationToken);
        }

        return MapToDto(message, currentUserId);
    }

    public async Task<MessageStatusUpdateDto?> MarkAsDeliveredAsync(
        Guid currentUserId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var delivery = await _dbContext.MessageDeliveries
            .Include(d => d.Message)
            .FirstOrDefaultAsync(d => d.MessageId == messageId && d.UserId == currentUserId, cancellationToken);

        if (delivery == null || delivery.Status >= DeliveryStatus.Delivered)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        delivery.Status = DeliveryStatus.Delivered;
        delivery.DeliveredAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MessageStatusUpdateDto(
            delivery.MessageId,
            delivery.Message?.ConversationId ?? Guid.Empty,
            DeliveryStatus.Delivered,
            now
        );
    }

    public async Task<List<MessageStatusUpdateDto>> MarkConversationAsReadAsync(
        Guid currentUserId,
        Guid conversationId,
        Guid? upToMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var isParticipant = await _dbContext.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId, cancellationToken);

        if (!isParticipant)
        {
            throw new ForbiddenAccessException("You are not a participant in this conversation.");
        }

        DateTime? cutoffSentAt = null;
        if (upToMessageId.HasValue)
        {
            var targetMsg = await _dbContext.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == upToMessageId.Value && m.ConversationId == conversationId, cancellationToken);

            if (targetMsg != null)
            {
                cutoffSentAt = targetMsg.SentAt;
            }
        }

        var deliveriesQuery = _dbContext.MessageDeliveries
            .Include(d => d.Message)
            .Where(d => d.UserId == currentUserId &&
                        d.Message != null &&
                        d.Message.ConversationId == conversationId &&
                        d.Status < DeliveryStatus.Read);

        if (cutoffSentAt.HasValue)
        {
            deliveriesQuery = deliveriesQuery.Where(d => d.Message!.SentAt <= cutoffSentAt.Value);
        }

        var unreadDeliveries = await deliveriesQuery.ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var updates = new List<MessageStatusUpdateDto>();

        foreach (var delivery in unreadDeliveries)
        {
            delivery.Status = DeliveryStatus.Read;
            delivery.ReadAt = now;
            if (delivery.DeliveredAt == null)
            {
                delivery.DeliveredAt = now;
            }

            updates.Add(new MessageStatusUpdateDto(
                delivery.MessageId,
                conversationId,
                DeliveryStatus.Read,
                now
            ));
        }

        // Update participant read state
        var participant = await _dbContext.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId, cancellationToken);

        if (participant != null)
        {
            participant.LastReadAt = now;
            if (upToMessageId.HasValue)
            {
                participant.LastReadMessageId = upToMessageId.Value;
            }
            else if (unreadDeliveries.Count > 0)
            {
                var highestMsg = unreadDeliveries.MaxBy(d => d.Message?.SentAt);
                if (highestMsg != null)
                {
                    participant.LastReadMessageId = highestMsg.MessageId;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return updates;
    }

    private static MessageDto MapToDto(Message message, Guid currentUserId)
    {
        var isMine = message.SenderId == currentUserId;

        DeliveryStatus status;
        DateTime? deliveredAt = null;
        DateTime? readAt = null;

        if (isMine)
        {
            // For sender: derive status from recipients
            if (message.Deliveries.Any(d => d.Status == DeliveryStatus.Read))
            {
                status = DeliveryStatus.Read;
                readAt = message.Deliveries.Where(d => d.ReadAt.HasValue).MaxBy(d => d.ReadAt)!.ReadAt;
                deliveredAt = message.Deliveries.Where(d => d.DeliveredAt.HasValue).MaxBy(d => d.DeliveredAt)?.DeliveredAt;
            }
            else if (message.Deliveries.Any(d => d.Status == DeliveryStatus.Delivered))
            {
                status = DeliveryStatus.Delivered;
                deliveredAt = message.Deliveries.Where(d => d.DeliveredAt.HasValue).MaxBy(d => d.DeliveredAt)!.DeliveredAt;
            }
            else
            {
                status = DeliveryStatus.Sent;
            }
        }
        else
        {
            // For recipient: look up my own delivery record
            var myDelivery = message.Deliveries.FirstOrDefault(d => d.UserId == currentUserId);
            status = myDelivery?.Status ?? DeliveryStatus.Sent;
            deliveredAt = myDelivery?.DeliveredAt;
            readAt = myDelivery?.ReadAt;
        }

        var media = message.MediaAttachments?.FirstOrDefault();

        string? replyContent = message.ReplyToMessage?.Content;
        if (string.IsNullOrWhiteSpace(replyContent) && message.ReplyToMessage != null)
        {
            replyContent = message.ReplyToMessage.MessageType switch
            {
                MessageType.Image => "📷 صورة",
                MessageType.Sticker => "🎨 ملصق",
                _ => "رسالة"
            };
        }

        string? replySenderName = null;
        if (message.ReplyToMessage != null)
        {
            replySenderName = message.ReplyToMessage.SenderId == currentUserId
                ? "أنت"
                : (message.ReplyToMessage.Sender?.FullName ?? message.ReplyToMessage.Sender?.UserName ?? "مستخدم");
        }

        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Sender?.UserName ?? "unknown",
            message.Sender?.FullName ?? "Unknown User",
            message.Sender?.AvatarUrl,
            message.Content,
            message.MessageType,
            message.ClientMessageId,
            message.SentAt,
            message.EditedAt,
            message.ReplyToMessageId,
            replyContent,
            status,
            deliveredAt,
            readAt,
            isMine,
            media?.FileUrl,
            media?.ThumbnailUrl,
            media?.FileSizeBytes,
            replySenderName
        );
    }
}
