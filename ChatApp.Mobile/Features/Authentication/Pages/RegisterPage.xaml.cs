using ChatApp.Mobile.Features.Authentication.ViewModels;

namespace ChatApp.Mobile.Features.Authentication.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage() : this(IPlatformApplication.Current?.Services?.GetService<RegisterViewModel>() ?? new RegisterViewModel(null!, null!, null!))
    {
    }

    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
