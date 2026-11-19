using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Features.Conversations.Services;
using ChatApp.Mobile.Features.Conversations.ViewModels;
using ChatApp.Mobile.Navigation;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.ViewModels;

public class ConversationsViewModelTests
{
    private readonly Mock<IConversationApiService> _conversationServiceMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly ConversationsViewModel _sut;

    public ConversationsViewModelTests()
    {
        _conversationServiceMock = new Mock<IConversationApiService>();
        _authStateMock = new Mock<IAuthenticationStateService>();
        _navigationMock = new Mock<INavigationService>();

        _authStateMock.Setup(x => x.CurrentUser).Returns(new UserProfileDto(
            Guid.NewGuid(),
            "current_user",
            "Current User",
            "current@example.com",
            null,
            null,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        ));

        _sut = new ConversationsViewModel(
            _conversationServiceMock.Object,
            _authStateMock.Object,
            _navigationMock.Object);
    }

    [Fact]
    public async Task LoadConversations_WhenSuccessful_ShouldPopulateConversationsAndFilteredList()
    {
        // Arrange
        var conversations = new List<ConversationSummaryDto>
        {
            new(
                Guid.NewGuid(),
                ConversationType.Direct,
                "Omar Ali",
                null,
                Guid.NewGuid(),
                "omar_ali",
                true,
                DateTime.UtcNow,
                new LastMessageDto(Guid.NewGuid(), "Hello there!", MessageType.Text, DateTime.UtcNow, Guid.NewGuid(), "Omar Ali"),
                0,
                false,
                false,
                false,
                DateTime.UtcNow
            )
        };

        _conversationServiceMock
            .Setup(x => x.GetConversationsAsync())
            .ReturnsAsync(ApiResult<List<ConversationSummaryDto>>.Success(conversations));

        // Act
        await _sut.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        _sut.Conversations.Should().HaveCount(1);
        _sut.FilteredConversations.Should().HaveCount(1);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadConversations_WhenApiFails_ShouldSetErrorMessage()
    {
        // Arrange
        _conversationServiceMock
            .Setup(x => x.GetConversationsAsync())
            .ReturnsAsync(ApiResult<List<ConversationSummaryDto>>.Failure("فشل الاتصال بالخادم"));

        // Act
        await _sut.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Be("فشل الاتصال بالخادم");
        _sut.Conversations.Should().BeEmpty();
    }

    [Fact]
    public void SearchText_ShouldFilterConversationsLocallyByTitleAndContent()
    {
        // Arrange
        var conv1 = new ConversationSummaryDto(
            Guid.NewGuid(),
            ConversationType.Direct,
            "Ahmed Mohamed",
            null,
            Guid.NewGuid(),
            "ahmed",
            true,
            DateTime.UtcNow,
            new LastMessageDto(Guid.NewGuid(), "السلام عليكم", MessageType.Text, DateTime.UtcNow, Guid.NewGuid(), "Ahmed"),
            0,
            false,
            false,
            false,
            DateTime.UtcNow
        );

        var conv2 = new ConversationSummaryDto(
            Guid.NewGuid(),
            ConversationType.Direct,
            "Sara Ibrahim",
            null,
            Guid.NewGuid(),
            "sara",
            false,
            DateTime.UtcNow.AddMinutes(-10),
            new LastMessageDto(Guid.NewGuid(), "Are we meeting tomorrow?", MessageType.Text, DateTime.UtcNow, Guid.NewGuid(), "Sara"),
            1,
            false,
            false,
            false,
            DateTime.UtcNow
        );

        _sut.Conversations = new System.Collections.ObjectModel.ObservableCollection<ConversationSummaryDto> { conv1, conv2 };

        // Act & Assert 1: Search by title
        _sut.SearchText = "Ahmed";
        _sut.FilteredConversations.Should().HaveCount(1);
        _sut.FilteredConversations[0].Title.Should().Be("Ahmed Mohamed");

        // Act & Assert 2: Search by message text
        _sut.SearchText = "meeting";
        _sut.FilteredConversations.Should().HaveCount(1);
        _sut.FilteredConversations[0].Title.Should().Be("Sara Ibrahim");

        // Act & Assert 3: Clear search
        _sut.SearchText = string.Empty;
        _sut.FilteredConversations.Should().HaveCount(2);
    }

    [Fact]
    public async Task SelectConversation_ShouldNavigateToChatWithParameters()
    {
        // Arrange
        var convId = Guid.NewGuid();
        var conv = new ConversationSummaryDto(
            convId,
            ConversationType.Direct,
            "Sara Ibrahim",
            "https://avatar.com/sara.png",
            Guid.NewGuid(),
            "sara",
            true,
            DateTime.UtcNow,
            null,
            0,
            false,
            false,
            false,
            DateTime.UtcNow
        );

        // Act
        await _sut.SelectConversationCommand.ExecuteAsync(conv);

        // Assert
        _navigationMock.Verify(x => x.NavigateToAsync(
            AppRoutes.Chat,
            It.Is<IDictionary<string, object>>(p =>
                (Guid)p["ConversationId"] == convId &&
                (string)p["Title"] == "Sara Ibrahim" &&
                (string)p["AvatarUrl"] == "https://avatar.com/sara.png")),
            Times.Once);
    }

    [Fact]
    public async Task OpenUserSearch_ShouldNavigateToUserSearchRoute()
    {
        // Act
        await _sut.OpenUserSearchCommand.ExecuteAsync(null);

        // Assert
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.UserSearch, null), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldSetUnauthenticatedAndNavigateToWelcome()
    {
        // Act
        await _sut.LogoutCommand.ExecuteAsync(null);

        // Assert
        _authStateMock.Verify(x => x.SetUnauthenticated(), Times.Once);
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.Welcome, null), Times.Once);
    }
}
