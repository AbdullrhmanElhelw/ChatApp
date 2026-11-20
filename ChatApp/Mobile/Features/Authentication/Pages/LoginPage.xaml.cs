using ChatApp.Mobile.Features.Authentication.ViewModels;

namespace ChatApp.Mobile.Features.Authentication.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage() : this(IPlatformApplication.Current?.Services?.GetService<LoginViewModel>() ?? new LoginViewModel(null!, null!, null!))
    {
    }

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
