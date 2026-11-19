using ChatApp.Mobile.Features.Conversations.ViewModels;

namespace ChatApp.Mobile.Features.Conversations.Pages;

public partial class ConversationsPage : ContentPage
{
    private readonly ConversationsViewModel _viewModel;

    public ConversationsPage() : this(IPlatformApplication.Current?.Services?.GetService<ConversationsViewModel>() ?? new ConversationsViewModel(null!, null!, null!))
    {
    }

    public ConversationsPage(ConversationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadConversationsAsync();
    }
}
