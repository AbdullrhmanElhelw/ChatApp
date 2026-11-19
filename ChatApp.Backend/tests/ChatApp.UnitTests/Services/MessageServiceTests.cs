using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Features.Messages.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatApp.UnitTests.Services;

public class MessageServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SendMessage_WhenUserIsParticipant_ShouldPersistMessageAndDeliveries()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var sender = new AppUser { Id = senderId, UserName = "sender", FullName = "Sender User" };
        var recipient = new AppUser { Id = recipientId, UserName = "recipient", FullName = "Recipient User" };
        var conv = new Conversation { Id = conversationId, ConversationType = ConversationType.Direct, CreatedById = senderId };

        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = senderId });
        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = recipientId });

        db.Users.AddRange(sender, recipient);
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var clientMsgId = Guid.NewGuid();
        var request = new SendMessageRequestDto(conversationId, "Hello World!", MessageType.Text, clientMsgId);

        // Act
        var result = await sut.SendMessageAsync(senderId, request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello World!");
        result.SenderId.Should().Be(senderId);
        result.DeliveryStatus.Should().Be(DeliveryStatus.Sent);
        result.IsMine.Should().BeTrue();

        var persistedMsg = await db.Messages.Include(m => m.Deliveries).FirstOrDefaultAsync(m => m.Id == result.Id);
        persistedMsg.Should().NotBeNull();
        persistedMsg!.Deliveries.Should().HaveCount(1);
        persistedMsg.Deliveries.First().UserId.Should().Be(recipientId);
        persistedMsg.Deliveries.First().Status.Should().Be(DeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendMessage_WhenDuplicateClientMessageId_ShouldBeIdempotent()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var sender = new AppUser { Id = senderId, UserName = "sender", FullName = "Sender User" };
        var conv = new Conversation { Id = conversationId, CreatedById = senderId };
        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = senderId });
        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = recipientId });

        db.Users.Add(sender);
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var clientMsgId = Guid.NewGuid();
        var request = new SendMessageRequestDto(conversationId, "Original Content", MessageType.Text, clientMsgId);

        var firstCall = await sut.SendMessageAsync(senderId, request);

        // Act: send again with same ClientMessageId
        var secondRequest = new SendMessageRequestDto(conversationId, "Different Content", MessageType.Text, clientMsgId);
        var secondCall = await sut.SendMessageAsync(senderId, secondRequest);

        // Assert
        secondCall.Id.Should().Be(firstCall.Id);
        secondCall.Content.Should().Be("Original Content");
        (await db.Messages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SendMessage_WhenNotParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var nonMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var conv = new Conversation { Id = conversationId, CreatedById = Guid.NewGuid() };
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var request = new SendMessageRequestDto(conversationId, "Unauthorized Hello");

        // Act
        var act = () => sut.SendMessageAsync(nonMemberId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetMessages_WhenParticipant_ShouldReturnMessagesInChronologicalOrder()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var user1 = new AppUser { Id = user1Id, UserName = "user1", FullName = "User One" };
        var user2 = new AppUser { Id = user2Id, UserName = "user2", FullName = "User Two" };

        var conv = new Conversation { Id = conversationId, CreatedById = user1Id };
        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = user1Id, User = user1 });
        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = user2Id, User = user2 });

        var msg1 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = user1Id,
            Sender = user1,
            Content = "First message",
            SentAt = DateTime.UtcNow.AddMinutes(-10),
            ClientMessageId = Guid.NewGuid(),
            IsDeleted = false
        };

        var msg2 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = user2Id,
            Sender = user2,
            Content = "Second message",
            SentAt = DateTime.UtcNow.AddMinutes(-5),
            ClientMessageId = Guid.NewGuid(),
            IsDeleted = false
        };

        conv.Messages.Add(msg1);
        conv.Messages.Add(msg2);

        db.Users.AddRange(user1, user2);
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        // Act
        var result = await sut.GetMessagesAsync(user1Id, conversationId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("First message");
        result[1].Content.Should().Be("Second message");
    }

    [Fact]
    public async Task GetMessages_WhenNotParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);
        var conversationId = Guid.NewGuid();

        // Act
        var act = () => sut.GetMessagesAsync(Guid.NewGuid(), conversationId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task MarkAsDelivered_WhenValid_ShouldUpdateDeliveryStatusAndReturnUpdateDto()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var delivery = new MessageDelivery
        {
            MessageId = messageId,
            UserId = recipientId,
            Status = DeliveryStatus.Sent
        };

        var message = new Message
        {
            Id = messageId,
            ConversationId = conversationId,
            SenderId = senderId,
            Deliveries = new List<MessageDelivery> { delivery }
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        // Act
        var result = await sut.MarkAsDeliveredAsync(recipientId, messageId);

        // Assert
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(messageId);
        result.Status.Should().Be(DeliveryStatus.Delivered);

        var updatedDelivery = await db.MessageDeliveries.FirstAsync(d => d.MessageId == messageId && d.UserId == recipientId);
        updatedDelivery.Status.Should().Be(DeliveryStatus.Delivered);
        updatedDelivery.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkConversationAsRead_WhenValid_ShouldMarkUnreadMessagesAndReturnList()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var conv = new Conversation { Id = conversationId, CreatedById = senderId };
        var part = new ConversationParticipant { ConversationId = conversationId, UserId = recipientId };
        conv.Participants.Add(part);

        var msg1 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            SentAt = DateTime.UtcNow.AddMinutes(-5),
            ClientMessageId = Guid.NewGuid()
        };
        var delivery1 = new MessageDelivery
        {
            MessageId = msg1.Id,
            UserId = recipientId,
            Status = DeliveryStatus.Sent,
            Message = msg1
        };

        var msg2 = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            SentAt = DateTime.UtcNow.AddMinutes(-2),
            ClientMessageId = Guid.NewGuid()
        };
        var delivery2 = new MessageDelivery
        {
            MessageId = msg2.Id,
            UserId = recipientId,
            Status = DeliveryStatus.Sent,
            Message = msg2
        };

        db.Conversations.Add(conv);
        db.Messages.AddRange(msg1, msg2);
        db.MessageDeliveries.AddRange(delivery1, delivery2);
        await db.SaveChangesAsync();

        // Act
        var updates = await sut.MarkConversationAsReadAsync(recipientId, conversationId);

        // Assert
        updates.Should().HaveCount(2);
        updates.All(u => u.Status == DeliveryStatus.Read).Should().BeTrue();

        var updatedDeliveries = await db.MessageDeliveries.Where(d => d.UserId == recipientId).ToListAsync();
        updatedDeliveries.All(d => d.Status == DeliveryStatus.Read && d.ReadAt != null).Should().BeTrue();

        var updatedPart = await db.ConversationParticipants.FirstAsync(cp => cp.ConversationId == conversationId && cp.UserId == recipientId);
        updatedPart.LastReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ToggleReactionAsync_WhenSingleUserReacts_ShouldReturnCountOfOneNotTwo()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new MessageService(db);

        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var user = new AppUser { Id = userId, UserName = "ahmed", FullName = "Ahmed", Email = "a@test.com" };
        var conv = new Conversation { Id = conversationId, CreatedById = userId };
        conv.Participants.Add(new ConversationParticipant { ConversationId = conversationId, UserId = userId });

        var msg = new Message
        {
            Id = messageId,
            ConversationId = conversationId,
            SenderId = userId,
            Content = "Hello!",
            ClientMessageId = Guid.NewGuid()
        };

        db.Users.Add(user);
        db.Conversations.Add(conv);
        db.Messages.Add(msg);
        await db.SaveChangesAsync();

        // Act 1: Add reaction
        var result1 = await sut.ToggleReactionAsync(userId, messageId, "😂");

        // Assert 1: Count must be exactly 1
        result1.Added.Should().BeTrue();
        result1.Reactions.Should().ContainSingle();
        result1.Reactions[0].Emoji.Should().Be("😂");
        result1.Reactions[0].Count.Should().Be(1);
        result1.Reactions[0].HasReacted.Should().BeTrue();

        // Act 2: Change reaction to ❤️
        var result2 = await sut.ToggleReactionAsync(userId, messageId, "❤️");

        // Assert 2: Count must still be 1 for ❤️
        result2.Added.Should().BeTrue();
        result2.Reactions.Should().ContainSingle();
        result2.Reactions[0].Emoji.Should().Be("❤️");
        result2.Reactions[0].Count.Should().Be(1);

        // Act 3: Toggle off ❤️
        var result3 = await sut.ToggleReactionAsync(userId, messageId, "❤️");

        // Assert 3: Reactions should be empty
        result3.Added.Should().BeFalse();
        result3.Reactions.Should().BeEmpty();
    }
}
