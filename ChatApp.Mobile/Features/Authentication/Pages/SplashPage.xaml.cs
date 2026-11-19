using ChatApp.Mobile.Features.Authentication.ViewModels;

namespace ChatApp.Mobile.Features.Authentication.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel _viewModel;

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
        await _viewModel.InitializeAsync();
    }
}
