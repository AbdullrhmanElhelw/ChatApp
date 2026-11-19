using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Chat.ViewModels;

public partial class ChatViewModel : BaseViewModel, IQueryAttributable, IDisposable
{
    private readonly IChatHubService _chatHub;
    private readonly IMessageApiService _messageApi;
    private readonly IStickerApiService _stickerApi;
    private readonly IAuthenticationStateService _authState;
    private readonly INavigationService _navigationService;

    private CancellationTokenSource? _typingCts;
    private bool _isCurrentlyTyping;
    private bool _isDisposed;

    [ObservableProperty]
    public partial Guid ConversationId { get; set; }

    [ObservableProperty]
    public partial string ChatTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvatarUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<ChatMessageItem> Messages { get; set; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string MessageText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPartnerTyping { get; set; }

    [ObservableProperty]
    public partial string PartnerTypingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPartnerOnline { get; set; }

    [ObservableProperty]
    public partial string PartnerPresenceText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionStatusText))]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsAttachmentPanelVisible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<StickerPackClientDto> AvailableStickerPacks { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<StickerClientDto> ActiveStickers { get; set; } = new();

    public string ConnectionStatusText => IsConnected ? "متصل" : "جارٍ الاتصال...";

    public bool CanSend => !string.IsNullOrWhiteSpace(MessageText) && !IsBusy;

    public ChatViewModel(
        IChatHubService chatHub,
        IMessageApiService messageApi,
        IStickerApiService stickerApi,
        IAuthenticationStateService authState,
        INavigationService navigationService)
    {
        _chatHub = chatHub;
        _messageApi = messageApi;
        _stickerApi = stickerApi;
        _authState = authState;
        _navigationService = navigationService;

        SubscribeToHubEvents();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ConversationId", out var convIdObj))
        {
            if (convIdObj is Guid guid && guid != Guid.Empty)
            {
                ConversationId = guid;
            }
            else if (convIdObj is string str && Guid.TryParse(str, out var parsedGuid))
            {
                ConversationId = parsedGuid;
            }
        }

        if (query.TryGetValue("Title", out var titleObj) && titleObj is string title)
        {
            ChatTitle = title;
        }

        if (query.TryGetValue("AvatarUrl", out var avatarObj) && avatarObj is string avatar)
        {
            AvatarUrl = avatar;
        }
    }

    partial void OnConversationIdChanged(Guid value)
    {
        if (value != Guid.Empty)
        {
            _ = InitializeConversationAsync(value);
        }
    }

    partial void OnMessageTextChanged(string value)
    {
        HandleTypingDetection(value);
    }

    public async Task InitializeConversationAsync(Guid conversationId)
    {
        IsBusy = true;
        ClearError();

        try
        {
            // 1. Ensure SignalR connection is active
            if (!_chatHub.IsConnected)
            {
                await _chatHub.ConnectAsync();
            }

            IsConnected = _chatHub.IsConnected;

            // 2. Join the real-time conversation group
            await _chatHub.JoinConversationAsync(conversationId);

            // 3. Load historical messages via REST
            var historyResult = await _messageApi.GetMessagesAsync(conversationId);
            if (historyResult.IsSuccess && historyResult.Value != null)
            {
                Messages = new ObservableCollection<ChatMessageItem>(
                    historyResult.Value.Select(ChatMessageItem.FromDto));

                // 4. Mark unread messages as read
                await _chatHub.MarkAsReadAsync(conversationId);
            }
            else
            {
                ErrorMessage = historyResult.ErrorMessage;
            }

            // 5. Preload available sticker packs in background
            _ = LoadStickerPacksAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر تحميل المحادثة: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadStickerPacksAsync()
    {
        try
        {
            var publicPacksTask = _stickerApi.GetPublicPacksAsync();
            var myPacksTask = _stickerApi.GetMyPacksAsync();

            await Task.WhenAll(publicPacksTask, myPacksTask);

            var combinedPacks = new Dictionary<Guid, StickerPackClientDto>();

            var publicResult = await publicPacksTask;
            if (publicResult.IsSuccess && publicResult.Value != null)
            {
                foreach (var pack in publicResult.Value)
                {
                    combinedPacks[pack.Id] = pack;
                }
            }

            var myResult = await myPacksTask;
            if (myResult.IsSuccess && myResult.Value != null)
            {
                foreach (var pack in myResult.Value)
                {
                    combinedPacks[pack.Id] = pack;
                }
            }

            AvailableStickerPacks = new ObservableCollection<StickerPackClientDto>(combinedPacks.Values);

            var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;
            var allStickers = combinedPacks.Values
                .OrderByDescending(p => p.OwnerId == currentUserId)
                .ThenByDescending(p => p.CreatedAt)
                .SelectMany(p => p.Stickers)
                .ToList();

            ActiveStickers = new ObservableCollection<StickerClientDto>(allStickers);
        }
        catch { }
    }

    [RelayCommand]
    public void ToggleAttachmentPanel()
    {
        IsAttachmentPanelVisible = !IsAttachmentPanelVisible;
        if (IsAttachmentPanelVisible)
        {
            _ = LoadStickerPacksAsync();
        }
    }

    [RelayCommand]
    public async Task OpenStickerStudioAsync()
    {
        IsAttachmentPanelVisible = false;
        await _navigationService.NavigateToAsync(AppRoutes.StickerStudio);
    }

    [RelayCommand]
    public async Task SendStickerAsync(StickerClientDto? sticker)
    {
        if (sticker == null || ConversationId == Guid.Empty) return;

        IsAttachmentPanelVisible = false;

        var clientMessageId = Guid.NewGuid();
        var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;

        var optimisticItem = new ChatMessageItem
        {
            Id = Guid.Empty,
            ConversationId = ConversationId,
            SenderId = currentUserId,
            SenderUserName = _authState.CurrentUser?.UserName ?? "",
            SenderFullName = _authState.CurrentUser?.FullName ?? "",
            SenderAvatarUrl = _authState.CurrentUser?.AvatarUrl,
            Content = sticker.ImageUrl,
            MediaUrl = sticker.ImageUrl,
            MessageType = MessageType.Sticker,
            ClientMessageId = clientMessageId,
            SentAt = DateTime.UtcNow,
            IsMine = true,
            Status = MessageDeliveryStatus.Sent,
            IsSending = true
        };

        Messages.Add(optimisticItem);

        try
        {
            var request = new SendMessageRequestClientDto(
                ConversationId,
                sticker.ImageUrl,
                MessageType.Sticker,
                clientMessageId,
                StickerId: sticker.Id
            );

            await _chatHub.SendMessageAsync(request);
        }
        catch (Exception ex)
        {
            optimisticItem.IsSending = false;
            ErrorMessage = $"فشل إرسال الملصق: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task PickAndSendImageAsync()
    {
        IsAttachmentPanelVisible = false;

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "اختر صورة لإرسالها",
                FileTypes = FilePickerFileType.Images
            });

            if (file == null) return;

            IsBusy = true;
            await using var stream = await file.OpenReadAsync();
            var ext = Path.GetExtension(file.FullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            var uploadResult = await _stickerApi.UploadMediaAsync(stream, file.FileName, contentType, MessageType.Image);
            if (!uploadResult.IsSuccess || uploadResult.Value == null)
            {
                ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع الصورة.";
                return;
            }

            var media = uploadResult.Value;
            var clientMessageId = Guid.NewGuid();
            var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;

            var optimisticItem = new ChatMessageItem
            {
                Id = Guid.Empty,
                ConversationId = ConversationId,
                SenderId = currentUserId,
                SenderUserName = _authState.CurrentUser?.UserName ?? "",
                SenderFullName = _authState.CurrentUser?.FullName ?? "",
                SenderAvatarUrl = _authState.CurrentUser?.AvatarUrl,
                Content = null,
                MediaUrl = media.FileUrl,
                ThumbnailUrl = media.ThumbnailUrl,
                MediaSizeBytes = media.FileSizeBytes,
                MessageType = MessageType.Image,
                ClientMessageId = clientMessageId,
                SentAt = DateTime.UtcNow,
                IsMine = true,
                Status = MessageDeliveryStatus.Sent,
                IsSending = true
            };

            Messages.Add(optimisticItem);

            var request = new SendMessageRequestClientDto(
                ConversationId,
                Content: null,
                MessageType: MessageType.Image,
                ClientMessageId: clientMessageId,
                MediaAttachmentId: media.MediaId
            );

            await _chatHub.SendMessageAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر إرسال الصورة: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || ConversationId == Guid.Empty) return;

        var text = MessageText.Trim();
        MessageText = string.Empty;
        CancelTyping();

        var clientMessageId = Guid.NewGuid();
        var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;

        // Optimistic UI update: Show the message immediately with pending clock status
        var optimisticItem = new ChatMessageItem
        {
            Id = Guid.Empty,
            ConversationId = ConversationId,
            SenderId = currentUserId,
            SenderUserName = _authState.CurrentUser?.UserName ?? "",
            SenderFullName = _authState.CurrentUser?.FullName ?? "",
            SenderAvatarUrl = _authState.CurrentUser?.AvatarUrl,
            Content = text,
            MessageType = MessageType.Text,
            ClientMessageId = clientMessageId,
            SentAt = DateTime.UtcNow,
            IsMine = true,
            Status = MessageDeliveryStatus.Sent,
            IsSending = true
        };

        Messages.Add(optimisticItem);

        try
        {
            var request = new SendMessageRequestClientDto(
                ConversationId,
                text,
                MessageType.Text,
                clientMessageId
            );

            await _chatHub.SendMessageAsync(request);
        }
        catch (Exception ex)
        {
            optimisticItem.IsSending = false;
            ErrorMessage = $"فشل إرسال الرسالة: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task NavigateBackAsync()
    {
        if (ConversationId != Guid.Empty)
        {
            try
            {
                await _chatHub.LeaveConversationAsync(ConversationId);
            }
            catch { /* Ignore leave errors during back navigation */ }
        }

        await _navigationService.GoBackAsync();
    }

    private void SubscribeToHubEvents()
    {
        _chatHub.MessageReceived += OnMessageReceived;
        _chatHub.MessageConfirmationReceived += OnMessageConfirmationReceived;
        _chatHub.MessageStatusChanged += OnMessageStatusChanged;
        _chatHub.UserTyping += OnUserTyping;
        _chatHub.UserPresenceChanged += OnUserPresenceChanged;
        _chatHub.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnMessageReceived(MessageClientDto dto)
    {
        if (dto.ConversationId != ConversationId) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var existing = Messages.FirstOrDefault(m => m.Id == dto.Id || m.ClientMessageId == dto.ClientMessageId);
            if (existing == null)
            {
                var item = ChatMessageItem.FromDto(dto);
                Messages.Add(item);

                // Auto mark incoming message as read
                await _chatHub.MarkAsReadAsync(ConversationId, dto.Id);
            }
        });
    }

    private void OnMessageConfirmationReceived(MessageClientDto dto)
    {
        if (dto.ConversationId != ConversationId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Messages.FirstOrDefault(m => m.ClientMessageId == dto.ClientMessageId);
            if (item != null)
            {
                item.Id = dto.Id;
                item.IsSending = false;
                item.Status = dto.DeliveryStatus;
            }
        });
    }

    private void OnMessageStatusChanged(MessageStatusUpdateClientDto update)
    {
        if (update.ConversationId != ConversationId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Messages.FirstOrDefault(m => m.Id == update.MessageId);
            if (item != null && update.Status > item.Status)
            {
                item.Status = update.Status;
            }
        });
    }

    private void OnUserTyping(TypingIndicatorClientDto indicator)
    {
        if (indicator.ConversationId != ConversationId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPartnerTyping = indicator.IsTyping;
            PartnerTypingText = indicator.IsTyping ? $"{indicator.UserName} يكتب الآن..." : string.Empty;
        });
    }

    private void OnUserPresenceChanged(Guid userId, bool isOnline, DateTime? lastSeen)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPartnerOnline = isOnline;
            PartnerPresenceText = isOnline
                ? "متصل الآن"
                : (lastSeen.HasValue ? $"آخر ظهور {lastSeen.Value.ToLocalTime():hh:mm tt}" : "غير متصل");
        });
    }

    private void OnConnectionStateChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnected = isConnected;
        });
    }

    private void HandleTypingDetection(string currentText)
    {
        if (ConversationId == Guid.Empty) return;

        if (!string.IsNullOrWhiteSpace(currentText))
        {
            if (!_isCurrentlyTyping)
            {
                _isCurrentlyTyping = true;
                _ = _chatHub.SendTypingIndicatorAsync(ConversationId, true);
            }

            // Reset 2-second debounce timer
            _typingCts?.Cancel();
            _typingCts = new CancellationTokenSource();
            var token = _typingCts.Token;

            Task.Delay(2000, token).ContinueWith(t =>
            {
                if (!t.IsCanceled && _isCurrentlyTyping)
                {
                    _isCurrentlyTyping = false;
                    _ = _chatHub.SendTypingIndicatorAsync(ConversationId, false);
                }
            }, TaskScheduler.Default);
        }
        else if (_isCurrentlyTyping)
        {
            CancelTyping();
        }
    }

    private void CancelTyping()
    {
        _typingCts?.Cancel();
        if (_isCurrentlyTyping)
        {
            _isCurrentlyTyping = false;
            if (ConversationId != Guid.Empty)
            {
                _ = _chatHub.SendTypingIndicatorAsync(ConversationId, false);
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _chatHub.MessageReceived -= OnMessageReceived;
            _chatHub.MessageConfirmationReceived -= OnMessageConfirmationReceived;
            _chatHub.MessageStatusChanged -= OnMessageStatusChanged;
            _chatHub.UserTyping -= OnUserTyping;
            _chatHub.UserPresenceChanged -= OnUserPresenceChanged;
            _chatHub.ConnectionStateChanged -= OnConnectionStateChanged;
            _typingCts?.Dispose();
            _isDisposed = true;
        }
    }
}
