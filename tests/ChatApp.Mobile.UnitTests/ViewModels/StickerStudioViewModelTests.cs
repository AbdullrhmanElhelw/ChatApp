using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Stickers.ViewModels;
using ChatApp.Mobile.Navigation;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChatApp.Mobile.UnitTests.ViewModels;

public class StickerStudioViewModelTests
{
    private readonly Mock<IStickerApiService> _stickerApiMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly Mock<IAuthenticationStateService> _authStateMock;
    private readonly StickerStudioViewModel _sut;

    public StickerStudioViewModelTests()
    {
        _stickerApiMock = new Mock<IStickerApiService>();
        _navigationMock = new Mock<INavigationService>();
        _authStateMock = new Mock<IAuthenticationStateService>();

        _sut = new StickerStudioViewModel(_stickerApiMock.Object, _navigationMock.Object, _authStateMock.Object);
    }

    [Fact]
    public async Task Initialize_ShouldLoadUserStickerPacks()
    {
        // Arrange
        var packId = Guid.NewGuid();
        var packs = new List<StickerPackClientDto>
        {
            new(
                Id: packId,
                OwnerId: Guid.NewGuid(),
                OwnerName: "Artist",
                Name: "My Custom Pack",
                Description: "Fun pack",
                CoverImageUrl: "https://example.com/cover.png",
                IsPublic: true,
                StickerCount: 1,
                Stickers: new List<StickerClientDto>(),
                CreatedAt: DateTime.UtcNow
            )
        };

        _stickerApiMock.Setup(x => x.GetMyPacksAsync())
            .ReturnsAsync(ApiResult<List<StickerPackClientDto>>.Success(packs));

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.StickerPacks.Should().HaveCount(1);
        _sut.SelectedPack.Should().NotBeNull();
        _sut.SelectedPack!.Id.Should().Be(packId);
    }

    [Fact]
    public void SelectColor_ShouldUpdateSelectedColor()
    {
        // Act
        _sut.SelectColorCommand.Execute("#EF4444");

        // Assert
        _sut.SelectedColor.Should().Be("#EF4444");
    }

    [Fact]
    public void AddQuickEmoji_ShouldAppendToCaption()
    {
        // Arrange
        _sut.CaptionText = "Hello ";

        // Act
        _sut.AddQuickEmojiCommand.Execute("🚀");

        // Assert
        _sut.CaptionText.Should().Be("Hello 🚀");
    }

    [Fact]
    public async Task SaveSticker_WhenNoImageSelected_ShouldSetErrorMessage()
    {
        // Arrange
        _sut.SelectedImagePath = null;

        // Act
        await _sut.SaveStickerCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task NavigateBack_ShouldGoBack()
    {
        // Act
        await _sut.NavigateBackCommand.ExecuteAsync(null);

        // Assert
        _navigationMock.Verify(x => x.GoBackAsync(), Times.Once);
    }
}
