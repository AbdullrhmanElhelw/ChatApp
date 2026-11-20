using ChatApp.Application.Common.Exceptions;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatApp.UnitTests.Services;

public class ConversationServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_WithSelf_ShouldThrowValidationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new ConversationService(db);
        var userId = Guid.NewGuid();

        // Act
        var act = () => sut.GetOrCreateDirectConversationAsync(userId, userId);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("RecipientId");
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_WithNonExistentRecipient_ShouldThrowNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new ConversationService(db);
        var currentUserId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        // Act
        var act = () => sut.GetOrCreateDirectConversationAsync(currentUserId, recipientId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_WhenNew_ShouldCreateDirectConversationWithTwoParticipants()
    {
        // Arrange
        using var db = CreateDbContext();
        var currentUser = new AppUser { Id = Guid.NewGuid(), UserName = "user_a", FullName = "User A", Email = "a@test.com" };
        var recipientUser = new AppUser { Id = Guid.NewGuid(), UserName = "user_b", FullName = "User B", Email = "b@test.com" };
        db.Users.AddRange(currentUser, recipientUser);
        await db.SaveChangesAsync();

        var sut = new ConversationService(db);

        // Act
        var result = await sut.GetOrCreateDirectConversationAsync(currentUser.Id, recipientUser.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ConversationType.Should().Be(ConversationType.Direct);
        result.Data.Title.Should().Be("User B"); // Shows other user's name
        result.Data.Participants.Should().HaveCount(2);
        result.Data.Participants.Select(p => p.UserId).Should().Contain(new[] { currentUser.Id, recipientUser.Id });
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_WhenAlreadyExists_ShouldReturnExistingConversationIdempotently()
    {
        // Arrange
        using var db = CreateDbContext();
        var userA = new AppUser { Id = Guid.NewGuid(), UserName = "user_a", FullName = "User A", Email = "a@test.com" };
        var userB = new AppUser { Id = Guid.NewGuid(), UserName = "user_b", FullName = "User B", Email = "b@test.com" };
        db.Users.AddRange(userA, userB);

        var existingConv = new Conversation
        {
            Id = Guid.NewGuid(),
            ConversationType = ConversationType.Direct,
            CreatedById = userA.Id,
            CreatedAt = DateTime.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userA.Id, Role = ParticipantRole.Admin, JoinedAt = DateTime.UtcNow },
                new() { UserId = userB.Id, Role = ParticipantRole.Member, JoinedAt = DateTime.UtcNow }
            }
        };
        db.Conversations.Add(existingConv);
        await db.SaveChangesAsync();

        var sut = new ConversationService(db);

        // Act
        var result = await sut.GetOrCreateDirectConversationAsync(userA.Id, userB.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(existingConv.Id);
        (await db.Conversations.CountAsync()).Should().Be(1); // No duplicate created
    }

    [Fact]
    public async Task GetConversationById_WhenUserIsNotParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var db = CreateDbContext();
        var userA = new AppUser { Id = Guid.NewGuid(), UserName = "user_a", FullName = "User A", Email = "a@test.com" };
        var userB = new AppUser { Id = Guid.NewGuid(), UserName = "user_b", FullName = "User B", Email = "b@test.com" };
        var outsider = new AppUser { Id = Guid.NewGuid(), UserName = "outsider", FullName = "Outsider", Email = "out@test.com" };
        db.Users.AddRange(userA, userB, outsider);

        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            ConversationType = ConversationType.Direct,
            CreatedById = userA.Id,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userA.Id, Role = ParticipantRole.Admin },
                new() { UserId = userB.Id, Role = ParticipantRole.Member }
            }
        };
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var sut = new ConversationService(db);

        // Act
        var act = () => sut.GetConversationByIdAsync(outsider.Id, conv.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetUserConversations_ShouldReturnSortedByPinnedAndRecent()
    {
        // Arrange
        using var db = CreateDbContext();
        var currentUser = new AppUser { Id = Guid.NewGuid(), UserName = "me", FullName = "Me", Email = "me@test.com" };
        var user1 = new AppUser { Id = Guid.NewGuid(), UserName = "u1", FullName = "Friend One", Email = "u1@test.com" };
        var user2 = new AppUser { Id = Guid.NewGuid(), UserName = "u2", FullName = "Friend Two", Email = "u2@test.com" };
        db.Users.AddRange(currentUser, user1, user2);

        // Conv 1: Normal, has recent message
        var conv1 = new Conversation
        {
            Id = Guid.NewGuid(),
            ConversationType = ConversationType.Direct,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = currentUser.Id, IsPinned = false },
                new() { UserId = user1.Id }
            },
            Messages = new List<Message>
            {
                new() { Id = Guid.NewGuid(), SenderId = user1.Id, Content = "Hello recent", SentAt = DateTime.UtcNow.AddMinutes(-5) }
            }
        };

        // Conv 2: Pinned, older message
        var conv2 = new Conversation
        {
            Id = Guid.NewGuid(),
            ConversationType = ConversationType.Direct,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = currentUser.Id, IsPinned = true },
                new() { UserId = user2.Id }
            },
            Messages = new List<Message>
            {
                new() { Id = Guid.NewGuid(), SenderId = user2.Id, Content = "Hello pinned", SentAt = DateTime.UtcNow.AddHours(-3) }
            }
        };

        db.Conversations.AddRange(conv1, conv2);
        await db.SaveChangesAsync();

        var sut = new ConversationService(db);

        // Act
        var result = await sut.GetUserConversationsAsync(currentUser.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Id.Should().Be(conv2.Id); // Pinned comes first!
        result.Data[1].Id.Should().Be(conv1.Id);
    }
}
