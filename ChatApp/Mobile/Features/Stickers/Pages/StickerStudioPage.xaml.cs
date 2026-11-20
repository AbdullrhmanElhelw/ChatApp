using ChatApp.Mobile.Features.Stickers.ViewModels;

namespace ChatApp.Mobile.Features.Stickers.Pages;

public partial class StickerStudioPage : ContentPage
{
    private readonly StickerStudioViewModel _viewModel;

    public StickerStudioPage() : this(IPlatformApplication.Current?.Services?.GetService<StickerStudioViewModel>() ?? new StickerStudioViewModel(null!, null!, null!))
    {
    }

    public StickerStudioPage(StickerStudioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.CaptureCanvasAsync = CaptureStickerCanvasPngAsync;
        BindingContext = _viewModel;
    }

    private async Task<byte[]?> CaptureStickerCanvasPngAsync()
    {
        try
        {
            if (StickerCanvas == null) return null;
            var screenshot = await StickerCanvas.CaptureAsync();
            if (screenshot == null) return null;
            await using var ms = new MemoryStream();
            await screenshot.CopyToAsync(ms, ScreenshotFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
