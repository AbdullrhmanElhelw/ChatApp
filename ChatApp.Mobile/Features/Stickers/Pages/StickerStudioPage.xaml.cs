using ChatApp.Mobile.Features.Stickers.ViewModels;

namespace ChatApp.Mobile.Features.Stickers.Pages;

public partial class StickerStudioPage : ContentPage
{
    private readonly StickerStudioViewModel _viewModel;

    public StickerStudioPage() : this(IPlatformApplication.Current?.Services?.GetService<StickerStudioViewModel>() ?? new StickerStudioViewModel(null!, null!))
    {
    }

    public StickerStudioPage(StickerStudioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
