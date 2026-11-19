using System.Collections.ObjectModel;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Chat.ViewModels;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Navigation;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.ViewModels;

public class ChatViewModelTests
{
    private readonly Mock<IChatHubService> _chatHubMock;
    private readonly Mock<IMessageApiService> _messageApiMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly ChatViewModel _sut;
    private readonly Guid _currentUserId;

    public ChatViewModelTests()
    {
        _chatHubMock = new Mock<IChatHubService>();
        _messageApiMock = new Mock<IMessageApiService>();
        _authStateMock = new Mock<IAuthenticationStateService>();
        _navigationMock = new Mock<INavigationService>();

        _currentUserId = Guid.NewGuid();
        _authStateMock.Setup(x => x.CurrentUser).Returns(new UserProfileDto(
            Id: _currentUserId,
            UserName: "john_doe",
            Email: "john@example.com",
            FullName: "John Doe",
            Bio: null,
            AvatarUrl: null,
            IsOnline: true,
            LastSeenAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        ));

        _sut = new ChatViewModel(
            _chatHubMock.Object,
            _messageApiMock.Object,
            _authStateMock.Object,
            _navigationMock.Object);
    }

    [Fact]
    public async Task InitializeConversation_ShouldConnectJoinAndLoadMessages()
    {
        // Arrange
        var convId = Guid.NewGuid();
        _chatHubMock.Setup(x => x.IsConnected).Returns(false);

        var history = new List<MessageClientDto>
        {
            new(
                Id: Guid.NewGuid(),
                ConversationId: convId,
                SenderId: Guid.NewGuid(),
                SenderUserName: "alice",
                SenderFullName: "Alice Wonder",
                SenderAvatarUrl: null,
                Content: "Hello John!",
                MessageType: MessageType.Text,
                ClientMessageId: Guid.NewGuid(),
                SentAt: DateTime.UtcNow.AddMinutes(-5),
                EditedAt: null,
                ReplyToMessageId: null,
                ReplyToContent: null,
                DeliveryStatus: MessageDeliveryStatus.Read,
                DeliveredAt: DateTime.UtcNow,
                ReadAt: DateTime.UtcNow,
                IsMine: false
            )
        };

        _messageApiMock
            .Setup(x => x.GetMessagesAsync(convId, null, 50))
            .ReturnsAsync(ApiResult<List<MessageClientDto>>.Success(history));

        // Act
        await _sut.InitializeConversationAsync(convId);

        // Assert
        _chatHubMock.Verify(x => x.ConnectAsync(), Times.Once);
        _chatHubMock.Verify(x => x.JoinConversationAsync(convId), Times.Once);
        _messageApiMock.Verify(x => x.GetMessagesAsync(convId, null, 50), Times.Once);
        _chatHubMock.Verify(x => x.MarkAsReadAsync(convId, null), Times.Once);
        _sut.Messages.Should().HaveCount(1);
        _sut.Messages[0].Content.Should().Be("Hello John!");
    }

    [Fact]
    public async Task SendMessage_WhenTextProvided_ShouldAddOptimisticItemAndCallHub()
    {
        // Arrange
        var convId = Guid.NewGuid();
        _sut.ConversationId = convId;
        _sut.MessageText = "Peace upon you!";

        // Act
        await _sut.SendMessageCommand.ExecuteAsync(null);

        // Assert
        _sut.Messages.Should().HaveCount(1);
        var msg = _sut.Messages[0];
        msg.Content.Should().Be("Peace upon you!");
        msg.IsMine.Should().BeTrue();
        msg.IsSending.Should().BeTrue();
        _sut.MessageText.Should().BeEmpty();

        _chatHubMock.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequestClientDto>(r =>
            r.ConversationId == convId &&
            r.Content == "Peace upon you!" &&
            r.ClientMessageId == msg.ClientMessageId)), Times.Once);
    }

    [Fact]
    public async Task NavigateBack_ShouldLeaveConversationAndGoBack()
    {
        // Arrange
        var convId = Guid.NewGuid();
        _sut.ConversationId = convId;

        // Act
        await _sut.NavigateBackCommand.ExecuteAsync(null);

        // Assert
        _chatHubMock.Verify(x => x.LeaveConversationAsync(convId), Times.Once);
        _navigationMock.Verify(x => x.GoBackAsync(), Times.Once);
    }

    [Fact]
    public void ApplyQueryAttributes_WhenGivenValidQuery_ShouldSetProperties()
    {
        // Arrange
        var convId = Guid.NewGuid();
        var query = new Dictionary<string, object>
        {
            { "ConversationId", convId },
            { "Title", "Momo" },
            { "AvatarUrl", "https://example.com/avatar.png" }
        };

        // Act
        _sut.ApplyQueryAttributes(query);

        // Assert
        _sut.ConversationId.Should().Be(convId);
        _sut.ChatTitle.Should().Be("Momo");
        _sut.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public void ApplyQueryAttributes_WhenGivenStringGuid_ShouldParseAndSetConversationId()
    {
        // Arrange
        var convId = Guid.NewGuid();
        var query = new Dictionary<string, object>
        {
            { "ConversationId", convId.ToString() },
            { "Title", "Walid" },
            { "AvatarUrl", "" }
        };

        // Act
        _sut.ApplyQueryAttributes(query);

        // Assert
        _sut.ConversationId.Should().Be(convId);
        _sut.ChatTitle.Should().Be("Walid");
        _sut.AvatarUrl.Should().BeEmpty();
    }
}
