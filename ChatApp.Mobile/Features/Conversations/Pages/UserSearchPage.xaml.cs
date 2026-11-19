using ChatApp.Mobile.Features.Conversations.ViewModels;

namespace ChatApp.Mobile.Features.Conversations.Pages;

public partial class UserSearchPage : ContentPage
{
    public UserSearchPage() : this(IPlatformApplication.Current?.Services?.GetService<UserSearchViewModel>() ?? new UserSearchViewModel(null!, null!))
    {
    }

    public UserSearchPage(UserSearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
