using System.Collections.Specialized;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.ViewModels;

namespace ChatApp.Mobile.Features.Chat.Pages;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel? _viewModel;
    private CancellationTokenSource? _longPressCts;
    private double _currentPanX;

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

    private void CancelPendingLongPress()
    {
        try
        {
            _longPressCts?.Cancel();
            _longPressCts?.Dispose();
        }
        catch { }
        finally
        {
            _longPressCts = null;
        }
    }

    private void OnMessageBubblePointerPressed(object? sender, PointerEventArgs e)
    {
        CancelPendingLongPress();

        var element = sender as VisualElement;
        if (element?.BindingContext is not ChatMessageItem msg || _viewModel == null)
        {
            return;
        }

        _longPressCts = new CancellationTokenSource();
        var token = _longPressCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(360, token);
                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (token.IsCancellationRequested) return;
                    try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
                    try
                    {
                        await element.ScaleToAsync(0.95, 65, Easing.CubicOut);
                        await element.ScaleToAsync(1.0, 85, Easing.SpringOut);
                    }
                    catch { }
                    _viewModel.OpenReactionPickerCommand.Execute(msg);
                });
            }
            catch (TaskCanceledException) { }
            catch { }
        }, token);
    }

    private void OnMessageBubblePointerReleased(object? sender, PointerEventArgs e)
    {
        CancelPendingLongPress();
    }

    private void OnMessageBubblePointerExited(object? sender, PointerEventArgs e)
    {
        CancelPendingLongPress();
    }

    private async void OnMessageBubblePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (sender is not VisualElement element) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _currentPanX = 0;
                break;

            case GestureStatus.Running:
                if (Math.Abs(e.TotalX) > 10 || Math.Abs(e.TotalY) > 10)
                {
                    CancelPendingLongPress();
                }

                if (Math.Abs(e.TotalX) > Math.Abs(e.TotalY))
                {
                    _currentPanX = Math.Clamp(e.TotalX, -75, 75);
                    element.TranslationX = _currentPanX;
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                CancelPendingLongPress();
                var finalX = _currentPanX;
                _currentPanX = 0;

                try
                {
                    await element.TranslateToAsync(0, 0, 180, Easing.SpringOut);
                }
                catch
                {
                    element.TranslationX = 0;
                }

                if (Math.Abs(finalX) >= 35 && element.BindingContext is ChatMessageItem msg && _viewModel != null)
                {
                    try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                    _viewModel.StartReplyCommand.Execute(msg);
                    MessageInputEntry?.Focus();
                }
                break;
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
