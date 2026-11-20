using System.Collections.Specialized;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.ViewModels;
using ChatApp.Mobile.Features.Common;

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
        var perfStart = PerfLog.Start();
        InitializeComponent();
        PerfLog.Since("ChatPage.InitializeComponent", perfStart);
        _viewModel = viewModel;
        BindingContext = _viewModel;

        if (_viewModel?.Messages != null)
        {
            _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }

        if (_viewModel != null)
        {
            _viewModel.HeartPulseTriggered += OnHeartPulseTriggered;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        MessagesList.HandlerChanged += OnMessagesListHandlerChanged;
    }

    private void OnMessagesListHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        try
        {
            if (MessagesList.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView rv &&
                rv.GetLayoutManager() is AndroidX.RecyclerView.Widget.LinearLayoutManager llm)
            {
                llm.StackFromEnd = true;
            }
        }
        catch { }
#endif
        ScrollToBottom(animate: false);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_viewModel == null) return;
        if ((e.PropertyName == nameof(ChatViewModel.IsAttachmentPanelVisible) && _viewModel.IsAttachmentPanelVisible) ||
            (e.PropertyName == nameof(ChatViewModel.IsEmojiPickerVisible) && _viewModel.IsEmojiPickerVisible))
        {
            MainThread.BeginInvokeOnMainThread(DismissKeyboard);
        }
        else if (e.PropertyName == nameof(ChatViewModel.Messages) ||
                 e.PropertyName == nameof(ChatViewModel.DisplayedMessages))
        {
            if (e.PropertyName == nameof(ChatViewModel.Messages) && _viewModel.Messages != null)
            {
                _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
                _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
            }
            ScrollToBottom(animate: false);
        }
    }

    private void ScrollToBottom(bool animate)
    {
        if (_viewModel?.DisplayedMessages == null || _viewModel.DisplayedMessages.Count == 0) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Task.Delay(50);
                var items = _viewModel?.DisplayedMessages;
                if (items != null && items.Count > 0 && MessagesList != null)
                {
                    var lastItem = items[^1];
                    MessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: animate);
                }

                await Task.Delay(160);
                items = _viewModel?.DisplayedMessages;
                if (items != null && items.Count > 0 && MessagesList != null)
                {
                    MessagesList.ScrollTo(items[^1], position: ScrollToPosition.End, animate: false);
                }
            }
            catch { }
        });
    }

    private void DismissKeyboard()
    {
        try
        {
            MessageInputEntry?.Unfocus();
#if ANDROID
            var activity = Platform.CurrentActivity;
            var token = (MessageInputEntry?.Handler?.PlatformView as Android.Views.View)?.WindowToken
                        ?? activity?.CurrentFocus?.WindowToken;
            if (activity != null && token != null)
            {
                var imm = (Android.Views.InputMethods.InputMethodManager?)activity.GetSystemService(Android.Content.Context.InputMethodService);
                imm?.HideSoftInputFromWindow(token, Android.Views.InputMethods.HideSoftInputFlags.None);
            }
#endif
        }
        catch { }
    }

    private void OnHeartPulseTriggered()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (HeartOverlayGrid == null) return;
                if (!Resources.TryGetValue("IconHeart", out var iconObj) &&
                    Application.Current?.Resources.TryGetValue("IconHeart", out iconObj) != true)
                {
                    return;
                }

                if (iconObj is not Microsoft.Maui.Controls.Shapes.Geometry heartGeo) return;

                var offsets = new[] { -75.0, -35.0, 0.0, 35.0, 75.0 };
                var targetYs = new[] { -420.0, -520.0, -580.0, -490.0, -440.0 };
                var colors = new[] { "#FF4FB8", "#9B3BF9", "#FF4FB8", "#FFD6F2", "#7B2DF3" };

                for (var i = 0; i < offsets.Length; i++)
                {
                    var index = i;
                    var heart = new Microsoft.Maui.Controls.Shapes.Path
                    {
                        Data = heartGeo,
                        Fill = new SolidColorBrush(Color.FromArgb(colors[index])),
                        WidthRequest = 38,
                        HeightRequest = 38,
                        Aspect = Stretch.Uniform,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.End,
                        TranslationX = offsets[index],
                        TranslationY = -70,
                        Scale = 0.5,
                        Opacity = 1.0
                    };

                    HeartOverlayGrid.Children.Add(heart);

                    _ = AnimateFloatingHeartAsync(heart, offsets[index], targetYs[index], index * 90);
                }
            }
            catch { }
        });
    }

    private async Task AnimateFloatingHeartAsync(Microsoft.Maui.Controls.Shapes.Path heart, double startX, double targetY, int delayMs)
    {
        try
        {
            await Task.Delay(30 + delayMs);
            await heart.ScaleToAsync(1.6, 350, Easing.SpringOut);
            await Task.WhenAll(
                heart.TranslateToAsync(startX * 1.35, targetY, 1200, Easing.CubicOut),
                heart.FadeToAsync(0, 1200, Easing.CubicIn));
        }
        catch { }
        finally
        {
            HeartOverlayGrid?.Children.Remove(heart);
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _viewModel?.DisplayedMessages?.Count > 0)
        {
            ScrollToBottom(animate: false);
        }
    }

    private void OnMessageInputEntryFocused(object? sender, FocusEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsAttachmentPanelVisible = false;
            _viewModel.IsEmojiPickerOpen = false;
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
        ScrollToBottom(animate: false);
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
