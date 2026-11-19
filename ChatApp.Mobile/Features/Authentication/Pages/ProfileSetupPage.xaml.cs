using ChatApp.Mobile.Features.Authentication.ViewModels;

namespace ChatApp.Mobile.Features.Authentication.Pages;

public partial class ProfileSetupPage : ContentPage
{
    public ProfileSetupPage() : this(IPlatformApplication.Current?.Services?.GetService<ProfileSetupViewModel>() ?? new ProfileSetupViewModel(null!, null!))
    {
    }

    public ProfileSetupPage(ProfileSetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
