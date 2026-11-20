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

public class UserSearchViewModelTests
{
    private readonly Mock<IConversationApiService> _conversationServiceMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly UserSearchViewModel _sut;

    public UserSearchViewModelTests()
    {
        _conversationServiceMock = new Mock<IConversationApiService>();
        _authStateMock = new Mock<IAuthenticationStateService>();
        _navigationMock = new Mock<INavigationService>();

        _authStateMock.Setup(x => x.CurrentUser).Returns(new UserProfileDto(
            Id: Guid.NewGuid(),
            UserName: "logged_in_user",
            Email: "logged@example.com",
            FullName: "Logged In User",
            Bio: null,
            AvatarUrl: null,
            IsOnline: true,
            LastSeenAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        ));

        _sut = new UserSearchViewModel(
            _conversationServiceMock.Object,
            _authStateMock.Object,
            _navigationMock.Object);
    }

    [Fact]
    public async Task Search_WhenQueryIsEmpty_ShouldClearResultsWithoutListingAllUsers()
    {
        // Arrange
        _sut.SearchQuery = "   ";
        _conversationServiceMock.Invocations.Clear();

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _conversationServiceMock.Verify(x => x.SearchUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _sut.SearchResults.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WhenSearchingForSelf_ShouldSetErrorMessageAndNotCallApi()
    {
        // Arrange
        _sut.SearchQuery = "logged_in_user";

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("حسابك الحالي");
        _conversationServiceMock.Verify(x => x.SearchUsersAsync("logged_in_user", It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Search_WhenSuccessful_ShouldPopulateSearchResults()
    {
        // Arrange
        _sut.SearchQuery = "ahmed";
        var users = new List<UserProfileDto>
        {
            new(
                Guid.NewGuid(),
                "ahmed_dev",
                "Ahmed Developer",
                "ahmed@example.com",
                null,
                "Hey, using ChatApp!",
                true,
                DateTime.UtcNow,
                DateTime.UtcNow
            )
        };

        _conversationServiceMock
            .Setup(x => x.SearchUsersAsync("ahmed", 1, 20))
            .ReturnsAsync(ApiResult<List<UserProfileDto>>.Success(users));

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.SearchResults.Should().HaveCount(1);
        _sut.SearchResults[0].UserName.Should().Be("ahmed_dev");
        _sut.HasSearched.Should().BeTrue();
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsSearching.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task Search_WhenNoUsersFound_ShouldHaveEmptyResultsAndIsEmptyTrue()
    {
        // Arrange
        _sut.SearchQuery = "nonexistent";
        _conversationServiceMock
            .Setup(x => x.SearchUsersAsync("nonexistent", It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(ApiResult<List<UserProfileDto>>.Success(new List<UserProfileDto>()));

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.SearchResults.Should().BeEmpty();
        _sut.HasSearched.Should().BeTrue();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task Search_WhenApiFails_ShouldSetErrorMessage()
    {
        // Arrange
        _sut.SearchQuery = "error";
        _conversationServiceMock
            .Setup(x => x.SearchUsersAsync("error", It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(ApiResult<List<UserProfileDto>>.Failure("خطأ في الخادم"));

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Be("خطأ في الخادم");
        _sut.SearchResults.Should().BeEmpty();
    }

    [Fact]
    public async Task StartConversation_WhenSuccessful_ShouldCallApiAndNavigateToChat()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var convId = Guid.NewGuid();
        var targetUser = new UserProfileDto(
            Id: targetUserId,
            UserName: "target_user",
            Email: "target@example.com",
            FullName: "Target User",
            Bio: null,
            AvatarUrl: "https://avatar.com/target.png",
            IsOnline: true,
            LastSeenAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );

        var convDetail = new ConversationDetailDto(
            convId,
            ConversationType.Direct,
            "Target User",
            "https://avatar.com/target.png",
            DateTime.UtcNow,
            new List<ConversationParticipantDto>()
        );

        _conversationServiceMock
            .Setup(x => x.GetOrCreateDirectConversationAsync(targetUserId))
            .ReturnsAsync(ApiResult<ConversationDetailDto>.Success(convDetail));

        IDictionary<string, object>? capturedParams = null;
        _navigationMock
            .Setup(x => x.NavigateToAsync(AppRoutes.Chat, It.IsAny<IDictionary<string, object>?>()))
            .Callback<string, IDictionary<string, object>?>((route, dict) => capturedParams = dict)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.StartConversationCommand.ExecuteAsync(targetUser);

        // Assert
        _conversationServiceMock.Verify(x => x.GetOrCreateDirectConversationAsync(targetUserId), Times.Once);
        _navigationMock.Verify(x => x.NavigateToAsync(AppRoutes.Chat, It.IsAny<IDictionary<string, object>?>()), Times.Once);
        capturedParams.Should().NotBeNull();
        capturedParams!["ConversationId"].Should().Be(convId);
        capturedParams["Title"].Should().Be("Target User");
        capturedParams["AvatarUrl"].Should().Be("https://avatar.com/target.png");
    }

    [Fact]
    public async Task StartConversation_WhenApiFails_ShouldSetErrorMessage()
    {
        // Arrange
        var targetUser = new UserProfileDto(
            Id: Guid.NewGuid(),
            UserName: "target_user",
            Email: "target@example.com",
            FullName: "Target User",
            Bio: null,
            AvatarUrl: null,
            IsOnline: false,
            LastSeenAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );

        _conversationServiceMock
            .Setup(x => x.GetOrCreateDirectConversationAsync(targetUser.Id))
            .ReturnsAsync(ApiResult<ConversationDetailDto>.Failure("فشل إنشاء المحادثة"));

        // Act
        await _sut.StartConversationCommand.ExecuteAsync(targetUser);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Be("فشل إنشاء المحادثة");
        _navigationMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()), Times.Never);
    }

    [Fact]
    public async Task NavigateBack_ShouldCallGoBackAsync()
    {
        // Act
        await _sut.NavigateBackCommand.ExecuteAsync(null);

        // Assert
        _navigationMock.Verify(x => x.GoBackAsync(), Times.Once);
    }
}
