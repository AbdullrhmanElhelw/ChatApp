using System.Collections.Specialized;
using ChatApp.Mobile.Features.Chat.ViewModels;

namespace ChatApp.Mobile.Features.Chat.Pages;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel? _viewModel;

    public ChatPage() : this(IPlatformApplication.Current?.Services?.GetService<ChatViewModel>() ?? new ChatViewModel(null!, null!, null!, null!))
    {
    }

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        if (_viewModel?.Messages != null)
        {
            _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _viewModel?.Messages?.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Task.Delay(100);
                    if (_viewModel?.Messages?.Count > 0 && MessagesList != null)
                    {
                        MessagesList.ScrollTo(_viewModel.Messages.Count - 1, position: ScrollToPosition.End, animate: false);
                    }
                }
                catch
                {
                    // Catch WinUI CollectionView scroll measurement race conditions
                }
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel?.Messages != null)
        {
            _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        }
        _viewModel?.Dispose();
    }
}
