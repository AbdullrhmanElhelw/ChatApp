using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ChatApp.UnitTests.Entities;

public class EntityInitializationTests
{
    [Fact]
    public void AppUser_Initialization_ShouldHaveDefaultValues()
    {
        // Act
        var user = new AppUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            FullName = "Test User"
        };

        // Assert
        user.Id.Should().NotBeEmpty();
        user.IsOnline.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        user.RefreshTokens.Should().BeEmpty();
        user.Participations.Should().BeEmpty();
        user.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public void Message_Initialization_ShouldDefaultToTextMessage()
    {
        // Arrange & Act
        var message = new Message
        {
            ConversationId = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            Content = "Hello World!",
            ClientMessageId = Guid.NewGuid()
        };

        // Assert
        message.Id.Should().NotBeEmpty();
        message.MessageType.Should().Be(MessageType.Text);
        message.IsDeleted.Should().BeFalse();
        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UserRefreshToken_IsActive_ShouldBeTrue_WhenNotExpiredAndNotRevoked()
    {
        // Arrange
        var token = new UserRefreshToken
        {
            TokenHash = "sample_hash",
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Assert
        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void UserRefreshToken_IsActive_ShouldBeFalse_WhenRevoked()
    {
        // Arrange
        var token = new UserRefreshToken
        {
            TokenHash = "sample_hash",
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        // Assert
        token.IsActive.Should().BeFalse();
        token.IsRevoked.Should().BeTrue();
    }
}
