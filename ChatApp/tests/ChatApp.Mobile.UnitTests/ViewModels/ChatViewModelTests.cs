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
    private readonly Mock<IStickerApiService> _stickerApiMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly ChatViewModel _sut;
    private readonly Guid _currentUserId;

    public ChatViewModelTests()
    {
        _chatHubMock = new Mock<IChatHubService>();
        _messageApiMock = new Mock<IMessageApiService>();
        _stickerApiMock = new Mock<IStickerApiService>();
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
            _stickerApiMock.Object,
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

    [Fact]
    public async Task SendSticker_WhenStickerProvided_ShouldAddOptimisticItemAndCallHubWithStickerId()
    {
        // Arrange
        var convId = Guid.NewGuid();
        _sut.ConversationId = convId;
        var sticker = new StickerClientDto(Guid.NewGuid(), Guid.NewGuid(), "https://example.com/stk.webp", 512, 512, 0);

        // Act
        await _sut.SendStickerCommand.ExecuteAsync(sticker);

        // Assert
        _sut.Messages.Should().HaveCount(1);
        var msg = _sut.Messages[0];
        msg.MessageType.Should().Be(MessageType.Sticker);
        msg.IsSticker.Should().BeTrue();
        msg.StickerUrl.Should().Be("https://example.com/stk.webp");

        _chatHubMock.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequestClientDto>(r =>
            r.ConversationId == convId &&
            r.MessageType == MessageType.Sticker &&
            r.StickerId == sticker.Id)), Times.Once);
    }

    [Fact]
    public void ToggleAttachmentPanel_ShouldFlipVisibility()
    {
        // Arrange
        _sut.IsAttachmentPanelVisible = false;

        // Act
        _sut.ToggleAttachmentPanelCommand.Execute(null);

        // Assert
        _sut.IsAttachmentPanelVisible.Should().BeTrue();

        // Act 2
        _sut.ToggleAttachmentPanelCommand.Execute(null);

        // Assert 2
        _sut.IsAttachmentPanelVisible.Should().BeFalse();
    }

    [Fact]
    public async Task OpenStickerStudio_ShouldNavigateToStickerStudio()
    {
        // Act
        await _sut.OpenStickerStudioCommand.ExecuteAsync(null);

        // Assert
        _sut.IsAttachmentPanelVisible.Should().BeFalse();
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.StickerStudio, null), Times.Once);
    }

    [Fact]
    public void OpenReactionPicker_ShouldSetTargetMessageAndShowPicker()
    {
        // Arrange
        var message = new ChatMessageItem { Id = Guid.NewGuid(), Content = "Hello!" };

        // Act
        _sut.OpenReactionPickerCommand.Execute(message);

        // Assert
        _sut.ReactionTargetMessage.Should().Be(message);
        _sut.IsReactionPickerVisible.Should().BeTrue();
    }

    [Fact]
    public void CloseReactionPicker_ShouldClearTargetMessage()
    {
        // Arrange
        var message = new ChatMessageItem { Id = Guid.NewGuid(), Content = "Hello!" };
        _sut.OpenReactionPickerCommand.Execute(message);

        // Act
        _sut.CloseReactionPickerCommand.Execute(null);

        // Assert
        _sut.ReactionTargetMessage.Should().BeNull();
        _sut.IsReactionPickerVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ReactWithEmoji_ShouldInvokeHubAndClosePicker()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        var message = new ChatMessageItem { Id = msgId, Content = "Great work!" };
        _sut.OpenReactionPickerCommand.Execute(message);

        // Act
        await _sut.ReactWithEmojiCommand.ExecuteAsync("❤️");

        // Assert
        _sut.IsReactionPickerVisible.Should().BeFalse();
        _sut.ReactionTargetMessage.Should().BeNull();
        _chatHubMock.Verify(x => x.ReactToMessageAsync(msgId, "❤️"), Times.Once);
    }

    [Fact]
    public async Task ToggleReactionPill_ShouldInvokeHubWithEmojiAndMessageId()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        var reaction = new ReactionCountClientDto("🔥", 3, true, msgId);

        // Act
        await _sut.ToggleReactionPillCommand.ExecuteAsync(reaction);

        // Assert
        _chatHubMock.Verify(x => x.ReactToMessageAsync(msgId, "🔥"), Times.Once);
    }

    [Fact]
    public void SelectEmojiCategory_ShouldShowOnlyThatCategory()
    {
        // Act
        _sut.SelectEmojiCategoryCommand.Execute("الرموز");

        // Assert
        _sut.SelectedEmojiCategory.Should().Be("الرموز");
        _sut.FilteredEmojis.Should().Contain("❤️");
        _sut.FilteredEmojis.Should().NotContain("😀");
        _sut.EmojiCategoryTabs.Single(t => t.Key == "الرموز").IsSelected.Should().BeTrue();
        _sut.EmojiCategoryTabs.Single(t => t.Key == "الوجوه").IsSelected.Should().BeFalse();
    }

    [Fact]
    public void BackspaceMessageEmoji_ShouldRemoveLastGraphemeWithoutCorruptingSurrogatePairs()
    {
        // Arrange
        _sut.MessageText = "أهلاً 😂❤️";

        // Act
        _sut.BackspaceMessageEmojiCommand.Execute(null);

        // Assert
        _sut.MessageText.Should().Be("أهلاً 😂");

        // Act again
        _sut.BackspaceMessageEmojiCommand.Execute(null);

        // Assert
        _sut.MessageText.Should().Be("أهلاً ");
    }

    [Fact]
    public void InsertMessageEmoji_ShouldAppendEmojiToCurrentText()
    {
        // Arrange
        _sut.MessageText = "Hi ";

        // Act
        _sut.InsertMessageEmojiCommand.Execute("👋");

        // Assert
        _sut.MessageText.Should().Be("Hi 👋");
    }

    [Fact]
    public async Task ToggleReactionPill_WhenSameMessageIsAlreadyBeingUpdated_ShouldInvokeHubOnlyOnce()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var reaction = new ReactionCountClientDto("👍", 1, true, messageId);
        var hubCall = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _chatHubMock.Setup(x => x.ReactToMessageAsync(messageId, "👍")).Returns(hubCall.Task);

        // Act
        var firstRequest = _sut.ToggleReactionPillCommand.ExecuteAsync(reaction);
        await Task.Yield();
        await _sut.ToggleReactionPillCommand.ExecuteAsync(reaction);

        // Assert
        _chatHubMock.Verify(x => x.ReactToMessageAsync(messageId, "👍"), Times.Once);
        hubCall.SetResult(true);
        await firstRequest;
    }
}
