using System.Collections.Specialized;
using ChatApp.Mobile.Features.Chat.ViewModels;

namespace ChatApp.Mobile.Features.Chat.Pages;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel? _viewModel;

    public ChatPage() : this(IPlatformApplication.Current?.Services?.GetService<ChatViewModel>() ?? new ChatViewModel(null!, null!, null!, null!, null!))
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

    private void OnMessageInputEntryFocused(object? sender, FocusEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsAttachmentPanelVisible = false;
            _viewModel.CloseReactionPicker();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            _ = _viewModel.LoadStickerPacksAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // If navigating to a child page (like Sticker Studio), don't dispose the chat view model
        if (Navigation?.NavigationStack?.Count > 1)
        {
            return;
        }

        if (_viewModel?.Messages != null)
        {
            _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        }
        _viewModel?.Dispose();
    }
}
