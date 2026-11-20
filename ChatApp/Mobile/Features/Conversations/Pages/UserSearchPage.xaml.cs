using ChatApp.Mobile.Features.Conversations.ViewModels;

namespace ChatApp.Mobile.Features.Conversations.Pages;

public partial class UserSearchPage : ContentPage
{
    private readonly UserSearchViewModel _viewModel;

    public UserSearchPage() : this(IPlatformApplication.Current?.Services?.GetService<UserSearchViewModel>() ?? new UserSearchViewModel(null!, null!, null!))
    {
    }

    public UserSearchPage(UserSearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadInitialUsersAsync();
    }
}
