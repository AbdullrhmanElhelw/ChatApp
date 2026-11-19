using ChatApp.Mobile.Features.Authentication.ViewModels;

namespace ChatApp.Mobile.Features.Authentication.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage() : this(IPlatformApplication.Current?.Services?.GetService<WelcomeViewModel>() ?? new WelcomeViewModel(null!))
    {
    }

    public WelcomePage(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
