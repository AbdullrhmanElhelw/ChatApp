using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Conversations.DTOs;
using ChatApp.Application.Features.Conversations.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _dbContext;

    public ConversationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ConversationSummaryDto>>> GetUserConversationsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
            .Where(c => !c.IsDeleted &&
                        c.Participants.Any(p => p.UserId == currentUserId && !p.IsArchived))
            .ToListAsync(cancellationToken);

        var summaries = new List<ConversationSummaryDto>();

        foreach (var c in conversations)
        {
            var myParticipantInfo = c.Participants.First(p => p.UserId == currentUserId);
            var latestMessage = c.Messages.FirstOrDefault(m => !m.IsDeleted);

            string title = c.Title ?? string.Empty;
            string? avatarUrl = c.AvatarUrl;
            Guid? otherParticipantId = null;
            string? otherParticipantUserName = null;
            bool isOtherParticipantOnline = false;
            DateTime? otherParticipantLastSeenAt = null;

            if (c.ConversationType == ConversationType.Direct)
            {
                var otherPart = c.Participants.FirstOrDefault(x => x.UserId != currentUserId);
                if (otherPart?.User != null)
                {
                    title = otherPart.User.FullName;
                    avatarUrl = otherPart.User.AvatarUrl;
                    otherParticipantId = otherPart.User.Id;
                    otherParticipantUserName = otherPart.User.UserName;
                    isOtherParticipantOnline = otherPart.User.IsOnline;
                    otherParticipantLastSeenAt = otherPart.User.LastSeenAt;
                }
            }

            LastMessageDto? lastMessageDto = null;
            if (latestMessage != null)
            {
                lastMessageDto = new LastMessageDto(
                    latestMessage.Id,
                    latestMessage.Content,
                    latestMessage.MessageType,
                    latestMessage.SentAt,
                    latestMessage.SenderId,
                    latestMessage.Sender?.FullName ?? "Unknown"
                );
            }

            // Calculate unread count
            var readCutoff = myParticipantInfo.LastReadAt ?? myParticipantInfo.JoinedAt;
            var unreadCount = await _dbContext.Messages
                .AsNoTracking()
                .CountAsync(m => m.ConversationId == c.Id &&
                                 !m.IsDeleted &&
                                 m.SenderId != currentUserId &&
                                 m.SentAt > readCutoff, cancellationToken);

            var lastActivityAt = latestMessage?.SentAt ?? c.CreatedAt;

            summaries.Add(new ConversationSummaryDto(
                c.Id,
                c.ConversationType,
                title,
                avatarUrl,
                otherParticipantId,
                otherParticipantUserName,
                isOtherParticipantOnline,
                otherParticipantLastSeenAt,
                lastMessageDto,
                unreadCount,
                myParticipantInfo.IsPinned,
                myParticipantInfo.IsMuted,
                myParticipantInfo.IsArchived,
                lastActivityAt
            ));
        }

        var ordered = summaries
            .OrderByDescending(s => s.IsPinned)
            .ThenByDescending(s => s.LastActivityAt)
            .ToList();

        return ApiResponse<List<ConversationSummaryDto>>.Ok(ordered, "Conversations retrieved successfully.");
    }

    public async Task<ApiResponse<ConversationDetailDto>> GetOrCreateDirectConversationAsync(
        Guid currentUserId,
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == recipientId)
        {
            throw new ValidationException("RecipientId", "لا يمكن إنشاء محادثة مباشرة مع نفسك.");
        }

        var recipientExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == recipientId, cancellationToken);

        if (!recipientExists)
        {
            throw new NotFoundException("User", recipientId);
        }

        // Check if direct conversation already exists
        var existing = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.ConversationType == ConversationType.Direct && !c.IsDeleted)
            .Where(c => c.Participants.Any(p => p.UserId == currentUserId) &&
                        c.Participants.Any(p => p.UserId == recipientId))
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            return ApiResponse<ConversationDetailDto>.Ok(
                MapToDetailDto(existing, currentUserId),
                "Existing conversation retrieved.");
        }

        // Create new direct conversation
        var newConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ConversationType = ConversationType.Direct,
            CreatedById = currentUserId,
            CreatedAt = DateTime.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new()
                {
                    UserId = currentUserId,
                    Role = ParticipantRole.Admin,
                    JoinedAt = DateTime.UtcNow
                },
                new()
                {
                    UserId = recipientId,
                    Role = ParticipantRole.Member,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        _dbContext.Conversations.Add(newConversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload with users
        var created = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == newConversation.Id, cancellationToken);

        return ApiResponse<ConversationDetailDto>.Ok(
            MapToDetailDto(created, currentUserId),
            "Conversation created successfully.");
    }

    public async Task<ApiResponse<ConversationDetailDto>> GetConversationByIdAsync(
        Guid currentUserId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.Id == conversationId && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            throw new NotFoundException("Conversation", conversationId);
        }

        var isMember = conversation.Participants.Any(p => p.UserId == currentUserId);
        if (!isMember)
        {
            throw new ForbiddenAccessException("ليس لديك صلاحية الوصول إلى هذه المحادثة.");
        }

        return ApiResponse<ConversationDetailDto>.Ok(
            MapToDetailDto(conversation, currentUserId),
            "Conversation details retrieved.");
    }

    private static ConversationDetailDto MapToDetailDto(Conversation conversation, Guid currentUserId)
    {
        string title = conversation.Title ?? string.Empty;
        string? avatarUrl = conversation.AvatarUrl;

        if (conversation.ConversationType == ConversationType.Direct)
        {
            var otherUser = conversation.Participants
                .FirstOrDefault(p => p.UserId != currentUserId)?.User;

            if (otherUser != null)
            {
                title = otherUser.FullName;
                avatarUrl = otherUser.AvatarUrl;
            }
        }

        var participants = conversation.Participants
            .Select(p => new ConversationParticipantDto(
                p.UserId,
                p.User?.UserName ?? string.Empty,
                p.User?.FullName ?? "Unknown",
                p.User?.AvatarUrl,
                p.Role,
                p.User?.IsOnline ?? false,
                p.JoinedAt,
                p.LastReadAt
            ))
            .ToList();

        return new ConversationDetailDto(
            conversation.Id,
            conversation.ConversationType,
            title,
            avatarUrl,
            conversation.CreatedAt,
            participants
        );
    }
}
