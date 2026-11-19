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

        if (_viewModel != null)
        {
            _viewModel.HeartPulseTriggered += OnHeartPulseTriggered;
        }
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

                var offsets = new[] { -60.0, -25.0, 0.0, 30.0, 65.0 };
                var colors = new[] { "#FF4FB8", "#9B3BF9", "#FF4FB8", "#FFD6F2", "#7B2DF3" };
                var tasks = new List<Task>();

                for (var i = 0; i < offsets.Length; i++)
                {
                    var heart = new Microsoft.Maui.Controls.Shapes.Path
                    {
                        Data = heartGeo,
                        Fill = new SolidColorBrush(Color.FromArgb(colors[i])),
                        WidthRequest = 32,
                        HeightRequest = 32,
                        Aspect = Stretch.Uniform,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.End,
                        TranslationX = offsets[i],
                        TranslationY = -80,
                        Scale = 0.4,
                        Opacity = 0.95
                    };

                    HeartOverlayGrid.Children.Add(heart);

                    var delay = i * 80;
                    var targetY = -360 - (i * 35);
                    tasks.Add(Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Task.WhenAll(
                                heart.TranslateToAsync(offsets[i] * 1.4, targetY, 1100, Easing.CubicOut),
                                heart.ScaleToAsync(1.5, 550, Easing.SpringOut),
                                heart.FadeToAsync(0, 1100, Easing.CubicIn));
                            HeartOverlayGrid.Children.Remove(heart);
                        });
                    }));
                }

                await Task.WhenAll(tasks);
            }
            catch { }
        });
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
