using ChatApp.Mobile.Features.Authentication.ViewModels;

namespace ChatApp.Mobile.Features.Authentication.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel _viewModel;
    private CancellationTokenSource? _pulseCts;

    public SplashPage() : this(IPlatformApplication.Current?.Services?.GetService<SplashViewModel>() ?? new SplashViewModel(null!, null!, null!, null!))
    {
    }

    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _pulseCts?.Cancel();
        _pulseCts = new CancellationTokenSource();

        // Reset initial animation states
        SplashArtwork.Opacity = 0;
        SplashArtwork.Scale = 1.12;
        StatusContainer.Opacity = 0;
        StatusContainer.TranslationY = 24;
        VersionFooter.Opacity = 0;

        // Run entrance animations alongside initialization
        var entranceTask = RunEntranceAnimationsAsync(_pulseCts.Token);
        var initTask = _viewModel.InitializeAsync();

        await Task.WhenAll(entranceTask, initTask);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pulseCts?.Cancel();
        _pulseCts?.Dispose();
        _pulseCts = null;
    }

    private async Task RunEntranceAnimationsAsync(CancellationToken token)
    {
        try
        {
            // 1. Cinematic Ken Burns zoom-in & fade-in on artwork
            var artFade = SplashArtwork.FadeToAsync(1.0, 550, Easing.CubicOut);
            var artZoom = SplashArtwork.ScaleToAsync(1.0, 1500, Easing.CubicOut);

            // 2. Staggered slide-up for status pill & footer
            await Task.Delay(280, token);
            var statusFade = StatusContainer.FadeToAsync(1.0, 480, Easing.CubicOut);
            var statusSlide = StatusContainer.TranslateToAsync(0, 0, 520, Easing.CubicOut);
            var footerFade = VersionFooter.FadeToAsync(0.85, 600, Easing.CubicOut);

            await Task.WhenAll(artFade, artZoom, statusFade, statusSlide, footerFade);

            // 3. Start gentle ambient breathing loop while session check finishes
            _ = RunAmbientPulseLoopAsync(token);
        }
        catch (TaskCanceledException) { }
        catch { }
    }

    private async Task RunAmbientPulseLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll(
                    LogoAura.ScaleToAsync(1.10, 950, Easing.SinInOut),
                    LogoAura.FadeToAsync(0.28, 950, Easing.SinInOut));

                if (token.IsCancellationRequested) break;

                await Task.WhenAll(
                    LogoAura.ScaleToAsync(0.95, 950, Easing.SinInOut),
                    LogoAura.FadeToAsync(0.12, 950, Easing.SinInOut));
            }
        }
        catch (TaskCanceledException) { }
        catch { }
    }
}
