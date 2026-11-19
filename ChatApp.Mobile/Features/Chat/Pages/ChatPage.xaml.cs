using System.Collections.Specialized;
using ChatApp.Mobile.Features.Chat.ViewModels;

namespace ChatApp.Mobile.Features.Chat.Pages;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _viewModel.Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                if (_viewModel.Messages.Count > 0)
                {
                    MessagesList.ScrollTo(_viewModel.Messages.Count - 1, position: ScrollToPosition.End, animate: true);
                }
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        _viewModel.Dispose();
    }
}
