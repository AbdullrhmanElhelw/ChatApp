using ChatApp.Mobile.Features.Common;
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
        var perfStart = PerfLog.Start();
        InitializeComponent();
        PerfLog.Since("ConversationsPage.InitializeComponent", perfStart);
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadConversationsAsync();
    }
}
