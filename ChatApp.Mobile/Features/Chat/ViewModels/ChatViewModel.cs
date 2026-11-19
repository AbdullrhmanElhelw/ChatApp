using System.Collections.Concurrent;
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
    private readonly IVoiceAudioService? _voiceAudioService;
    private CancellationTokenSource? _recordingTimerCts;
    private ChatMessageItem? _currentlyPlayingAudioItem;

    private static readonly IReadOnlyDictionary<string, string[]> EmojiCatalog = new Dictionary<string, string[]>
    {
        ["الوجوه"] = ["😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇", "🙂", "🙃", "😉", "😍", "🥰", "😘", "😋", "😎", "🥳", "🤩", "🤔", "🤨", "😐", "😶", "🙄", "😏", "😴", "🥱", "😷", "🤒", "🤕", "🤢", "🤮", "🥵", "🥶", "😭", "😢", "😤", "😡", "🤯", "🥺", "😱", "😨", "😰", "😳", "🤗", "🤫", "🤭", "🫡", "🫠"],
        ["الأشخاص"] = ["👋", "🤚", "✋", "🖐️", "👌", "🤌", "🤏", "✌️", "🤞", "🫰", "🤟", "🤘", "🤙", "👈", "👉", "👆", "👇", "☝️", "👍", "👎", "✊", "👊", "🤛", "🤜", "👏", "🙌", "👐", "🤲", "🤝", "🙏", "💪", "🦾", "🧑", "👨", "👩", "🧒", "👶", "🧓", "👴", "👵", "🧔", "👮", "🧑‍🍳", "🧑‍🎓", "🧑‍💻", "🧑‍🎤", "🧑‍🚀", "🧑‍⚕️"],
        ["الحيوانات"] = ["🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", "🦁", "🐮", "🐷", "🐸", "🐵", "🙈", "🙉", "🙊", "🐔", "🐧", "🐦", "🦄", "🐝", "🦋", "🐢", "🐬", "🐳", "🦈", "🦖", "🐙", "🦀", "🐞", "🐠", "🦜", "🦚", "🐾", "🌸", "🌹", "🌻", "🌈", "🌙", "⭐", "⚡", "🔥", "🌊"],
        ["الطعام"] = ["🍏", "🍎", "🍐", "🍊", "🍋", "🍌", "🍉", "🍇", "🍓", "🫐", "🍒", "🍑", "🥭", "🍍", "🥥", "🥝", "🍅", "🥑", "🥦", "🥕", "🌽", "🌶️", "🍕", "🍔", "🍟", "🌭", "🍿", "🌮", "🌯", "🥙", "🍜", "🍣", "🍰", "🎂", "🍪", "🍩", "🍫", "🍦", "☕", "🧋", "🥤", "🍺", "🥂"],
        ["السفر"] = ["🚗", "🚕", "🚌", "🚎", "🏎️", "🚓", "🚑", "🚒", "🚲", "🛵", "🚆", "✈️", "🚀", "🛸", "🚁", "⛵", "🚢", "🗺️", "🌍", "🏖️", "🏝️", "🏕️", "🏠", "🏢", "🏰", "🗼", "🌃", "🌉", "🎡", "🎢", "⛺", "⛰️", "🏔️", "🌋", "⛲", "🗽"],
        ["الأنشطة"] = ["⚽", "🏀", "🏈", "⚾", "🎾", "🏐", "🏉", "🎱", "🏓", "🏸", "🥊", "🥋", "⛳", "🎣", "🤿", "🎽", "🛹", "🎿", "🏂", "🏋️", "🤸", "⛹️", "🤾", "🏊", "🎮", "🕹️", "🎯", "🎲", "🎸", "🎹", "🎤", "🎧", "🎨", "🎬", "🏆", "🥇", "🎉", "🎊"],
        ["الأشياء"] = ["⌚", "📱", "💻", "⌨️", "🖥️", "🖨️", "📷", "📹", "💡", "🔦", "📚", "📖", "✏️", "🖊️", "📝", "📌", "📎", "✂️", "🔒", "🔑", "🔨", "🛠️", "⚙️", "🎁", "🎈", "🪄", "💎", "🧸", "🛍️", "🎒", "🧢", "👓", "☂️", "🕯️"],
        ["الرموز"] = ["❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "❣️", "💕", "💞", "💓", "💗", "💖", "💘", "💝", "💟", "☮️", "✝️", "☪️", "🕉️", "♻️", "✅", "❌", "❓", "❗", "💯", "💢", "💬", "💤", "🔔", "🔕", "♨️", "🚫", "⭐", "✨", "💫", "🎵", "🎶"]
    };

    private readonly ConcurrentDictionary<Guid, byte> _reactionRequestsInFlight = new();
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
    [NotifyPropertyChangedFor(nameof(DisplayedMessages))]
    public partial bool IsSearchOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedMessages))]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCustomizeSheetVisible { get; set; }

    [ObservableProperty]
    public partial bool IsRecordingVoice { get; set; }

    [ObservableProperty]
    public partial string RecordingDurationText { get; set; } = "00:00";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomWallpaper))]
    public partial string? CustomWallpaperPath { get; set; }

    public bool HasCustomWallpaper => !string.IsNullOrWhiteSpace(CustomWallpaperPath);

    [ObservableProperty]
    public partial string SelectedWallpaperKey { get; set; } = "default";

    [ObservableProperty]
    public partial string SelectedBubbleKey { get; set; } = "violet";

    [ObservableProperty]
    public partial Color ChatBackgroundStartColor { get; set; } = Color.FromArgb("#13101A");

    [ObservableProperty]
    public partial Color ChatBackgroundEndColor { get; set; } = Color.FromArgb("#13101A");

    [ObservableProperty]
    public partial Color OutgoingBubbleStartColor { get; set; } = Color.FromArgb("#9B3BF9");

    [ObservableProperty]
    public partial Color OutgoingBubbleEndColor { get; set; } = Color.FromArgb("#7B2DF3");

    public ObservableCollection<ChatMessageItem> DisplayedMessages
    {
        get
        {
            if (!IsSearchOpen || string.IsNullOrWhiteSpace(SearchQuery))
            {
                return Messages;
            }

            var q = SearchQuery.Trim();
            return new ObservableCollection<ChatMessageItem>(
                Messages.Where(m => !string.IsNullOrWhiteSpace(m.Content) &&
                                    m.Content.Contains(q, StringComparison.CurrentCultureIgnoreCase)));
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string MessageText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPartnerPresence))]
    public partial bool IsPartnerTyping { get; set; }

    [ObservableProperty]
    public partial string PartnerTypingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPartnerOnline { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPartnerPresence))]
    public partial string PartnerPresenceText { get; set; } = string.Empty;

    public bool ShowPartnerPresence => !IsPartnerTyping && !string.IsNullOrWhiteSpace(PartnerPresenceText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionStatusText))]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsAttachmentPanelVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmojiPickerVisible))]
    public partial bool IsEmojiPickerOpen { get; set; }

    public bool IsEmojiPickerVisible => IsEmojiPickerOpen;

    [ObservableProperty]
    public partial ObservableCollection<string> EmojiCategories { get; set; } = new(EmojiCatalog.Keys);

    [ObservableProperty]
    public partial ObservableCollection<EmojiCategoryTabItem> EmojiCategoryTabs { get; set; } = new()
    {
        new EmojiCategoryTabItem { Key = "الوجوه", Icon = "😊", Title = "الوجوه", IsSelected = true },
        new EmojiCategoryTabItem { Key = "الأشخاص", Icon = "👋", Title = "الأشخاص", IsSelected = false },
        new EmojiCategoryTabItem { Key = "الحيوانات", Icon = "🦊", Title = "الحيوانات", IsSelected = false },
        new EmojiCategoryTabItem { Key = "الطعام", Icon = "🍕", Title = "الطعام", IsSelected = false },
        new EmojiCategoryTabItem { Key = "السفر", Icon = "✈️", Title = "السفر", IsSelected = false },
        new EmojiCategoryTabItem { Key = "الأنشطة", Icon = "⚽", Title = "الأنشطة", IsSelected = false },
        new EmojiCategoryTabItem { Key = "الأشياء", Icon = "💡", Title = "الأشياء", IsSelected = false },
        new EmojiCategoryTabItem { Key = "الرموز", Icon = "❤️", Title = "الرموز", IsSelected = false }
    };

    [ObservableProperty]
    public partial ObservableCollection<string> QuickEmojis { get; set; } = new()
    {
        "😂", "❤️", "🔥", "😍", "🥰", "👍", "😭", "🙏", "🥳", "💯"
    };

    [ObservableProperty]
    public partial ObservableCollection<string> FilteredEmojis { get; set; } = new(EmojiCatalog["الوجوه"]);

    [ObservableProperty]
    public partial string SelectedEmojiCategory { get; set; } = "الوجوه";

    [ObservableProperty]
    public partial ObservableCollection<StickerPackClientDto> AvailableStickerPacks { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<StickerClientDto> ActiveStickers { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReply))]
    public partial ChatMessageItem? ReplyingToMessage { get; set; }

    public bool HasReply => ReplyingToMessage != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReactionPickerVisible))]
    public partial ChatMessageItem? ReactionTargetMessage { get; set; }

    public bool IsReactionPickerVisible => ReactionTargetMessage != null;

    partial void OnSelectedEmojiCategoryChanged(string value)
    {
        if (EmojiCatalog.TryGetValue(value, out var emojis))
        {
            FilteredEmojis = new ObservableCollection<string>(emojis);
        }

        foreach (var tab in EmojiCategoryTabs)
        {
            tab.IsSelected = tab.Key == value;
        }
    }

    [RelayCommand]
    public void ToggleEmojiPicker()
    {
        IsEmojiPickerOpen = !IsEmojiPickerOpen;
        if (IsEmojiPickerOpen)
        {
            IsAttachmentPanelVisible = false;
        }
    }

    [RelayCommand]
    public void CloseEmojiPicker()
    {
        IsEmojiPickerOpen = false;
    }

    [RelayCommand]
    public void SelectEmojiCategory(string? category)
    {
        if (!string.IsNullOrWhiteSpace(category) && EmojiCatalog.ContainsKey(category))
        {
            SelectedEmojiCategory = category;
        }
    }

    [RelayCommand]
    public void InsertMessageEmoji(string? emoji)
    {
        if (!string.IsNullOrWhiteSpace(emoji))
        {
            MessageText += emoji;
        }
    }

    [RelayCommand]
    public void BackspaceMessageEmoji()
    {
        if (string.IsNullOrEmpty(MessageText)) return;

        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(MessageText);
        var elements = new List<string>();
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.GetTextElement());
        }

        if (elements.Count > 0)
        {
            elements.RemoveAt(elements.Count - 1);
            MessageText = string.Concat(elements);
        }
    }

    [RelayCommand]
    public void StartReply(ChatMessageItem message)
    {
        ReplyingToMessage = message;
    }

    [RelayCommand]
    public void CancelReply()
    {
        ReplyingToMessage = null;
    }

    [RelayCommand]
    public void OpenReactionPicker(ChatMessageItem? message)
    {
        if (message == null || message.Id == Guid.Empty) return;
        ReactionTargetMessage = message;
    }

    [RelayCommand]
    public void CloseReactionPicker()
    {
        ReactionTargetMessage = null;
    }

    [RelayCommand]
    public void ReplyFromReactionPicker()
    {
        if (ReactionTargetMessage != null)
        {
            ReplyingToMessage = ReactionTargetMessage;
        }
        CloseReactionPicker();
    }

    [RelayCommand]
    public async Task CopyTargetMessageAsync()
    {
        var content = ReactionTargetMessage?.Content;
        CloseReactionPicker();
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                await Clipboard.Default.SetTextAsync(content);
            }
            catch { }
        }
    }

    [RelayCommand]
    public async Task ReactWithEmojiAsync(string? emoji)
    {
        if (ReactionTargetMessage == null || string.IsNullOrWhiteSpace(emoji))
        {
            CloseReactionPicker();
            return;
        }

        var messageId = ReactionTargetMessage.Id;
        CloseReactionPicker();

        if (!_reactionRequestsInFlight.TryAdd(messageId, 0))
        {
            return;
        }

        try
        {
            await _chatHub.ReactToMessageAsync(messageId, emoji.Trim());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر إرسال التفاعل: {ex.Message}";
        }
        finally
        {
            _reactionRequestsInFlight.TryRemove(messageId, out _);
        }
    }

    [RelayCommand]
    public async Task ToggleReactionPillAsync(ReactionCountClientDto? reaction)
    {
        if (reaction == null || reaction.MessageId == Guid.Empty || string.IsNullOrWhiteSpace(reaction.Emoji))
            return;

        if (!_reactionRequestsInFlight.TryAdd(reaction.MessageId, 0))
        {
            return;
        }

        try
        {
            await _chatHub.ReactToMessageAsync(reaction.MessageId, reaction.Emoji.Trim());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر التفاعل: {ex.Message}";
        }
        finally
        {
            _reactionRequestsInFlight.TryRemove(reaction.MessageId, out _);
        }
    }

    public string ConnectionStatusText => IsConnected ? "متصل" : "جارٍ الاتصال...";

    public bool CanSend => !string.IsNullOrWhiteSpace(MessageText) && !IsBusy;

    public ChatViewModel(
        IChatHubService chatHub,
        IMessageApiService messageApi,
        IStickerApiService stickerApi,
        IAuthenticationStateService authState,
        INavigationService navigationService,
        IVoiceAudioService? voiceAudioService = null)
    {
        _chatHub = chatHub;
        _messageApi = messageApi;
        _stickerApi = stickerApi;
        _authState = authState;
        _navigationService = navigationService;
        _voiceAudioService = voiceAudioService;

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

    partial void OnMessagesChanged(ObservableCollection<ChatMessageItem> oldValue, ObservableCollection<ChatMessageItem> newValue)
    {
        if (oldValue != null)
            oldValue.CollectionChanged -= OnMessagesCollectionChanged;
        if (newValue != null)
            newValue.CollectionChanged += OnMessagesCollectionChanged;
        OnPropertyChanged(nameof(DisplayedMessages));
    }

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (IsSearchOpen && !string.IsNullOrWhiteSpace(SearchQuery))
        {
            OnPropertyChanged(nameof(DisplayedMessages));
        }
    }

    [RelayCommand]
    public void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        if (!IsSearchOpen)
        {
            SearchQuery = string.Empty;
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        IsSearchOpen = false;
    }

    [RelayCommand]
    public async Task ShowCallComingSoonAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlertAsync(
                "المكالمات الصوتية والمرئية",
                "ميزة مكالمات الصوت والفيديو في BetweenUs قادمة في التحديث القادم 💜",
                "حسناً");
        }
    }

    [RelayCommand]
    public async Task StartVoiceRecordingAsync()
    {
        if (_voiceAudioService == null || IsRecordingVoice) return;

        try
        {
            var started = await _voiceAudioService.StartRecordingAsync();
            if (!started)
            {
                ErrorMessage = "يرجى السماح بصلاحية الميكروفون لتسجيل الرسائل الصوتية.";
                return;
            }

            IsRecordingVoice = true;
            RecordingDurationText = "00:00";
            _recordingTimerCts?.Cancel();
            _recordingTimerCts = new CancellationTokenSource();
            var token = _recordingTimerCts.Token;
            var startTime = DateTime.UtcNow;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && IsRecordingVoice)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RecordingDurationText = elapsed.ToString(@"mm\:ss");
                    });
                    await Task.Delay(500, token);
                }
            }, token);
        }
        catch (Exception ex)
        {
            IsRecordingVoice = false;
            ErrorMessage = $"تعذر بدء التسجيل: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CancelVoiceRecordingAsync()
    {
        if (_voiceAudioService == null) return;
        _recordingTimerCts?.Cancel();
        IsRecordingVoice = false;
        RecordingDurationText = "00:00";
        await _voiceAudioService.StopRecordingAsync(cancel: true);
    }

    [RelayCommand]
    public async Task StopAndSendVoiceRecordingAsync()
    {
        if (_voiceAudioService == null || !IsRecordingVoice) return;

        _recordingTimerCts?.Cancel();
        IsRecordingVoice = false;
        var (filePath, duration) = await _voiceAudioService.StopRecordingAsync(cancel: false);
        RecordingDurationText = "00:00";

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || duration.TotalSeconds < 0.7)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await using var stream = File.OpenRead(filePath);
            var durationLabel = duration.ToString(@"mm\:ss");
            var uploadResult = await _stickerApi.UploadMediaAsync(stream, Path.GetFileName(filePath), "audio/mp4", MessageType.Audio);
            if (!uploadResult.IsSuccess || uploadResult.Value == null)
            {
                ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع الرسالة الصوتية.";
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
                Content = durationLabel,
                MediaUrl = media.FileUrl,
                MediaSizeBytes = media.FileSizeBytes,
                MessageType = MessageType.Audio,
                ClientMessageId = clientMessageId,
                SentAt = DateTime.UtcNow,
                IsMine = true,
                Status = MessageDeliveryStatus.Sent,
                IsSending = true
            };

            Messages.Add(optimisticItem);

            var request = new SendMessageRequestClientDto(
                ConversationId,
                Content: durationLabel,
                MessageType: MessageType.Audio,
                ClientMessageId: clientMessageId,
                MediaAttachmentId: media.MediaId
            );

            await _chatHub.SendMessageAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر إرسال الرسالة الصوتية: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
        }
    }

    [RelayCommand]
    public async Task TogglePlayAudioAsync(ChatMessageItem? item)
    {
        if (item == null || _voiceAudioService == null || string.IsNullOrWhiteSpace(item.DisplayAudioUrl)) return;

        try
        {
            if (item.IsPlayingAudio)
            {
                await _voiceAudioService.StopPlaybackAsync();
                item.IsPlayingAudio = false;
                _currentlyPlayingAudioItem = null;
                return;
            }

            if (_currentlyPlayingAudioItem != null)
            {
                _currentlyPlayingAudioItem.IsPlayingAudio = false;
            }

            _currentlyPlayingAudioItem = item;
            item.IsPlayingAudio = true;

            await _voiceAudioService.PlayAsync(item.DisplayAudioUrl, () =>
            {
                item.IsPlayingAudio = false;
                if (_currentlyPlayingAudioItem == item) _currentlyPlayingAudioItem = null;
            });
        }
        catch (Exception ex)
        {
            item.IsPlayingAudio = false;
            ErrorMessage = $"تعذر تشغيل الصوت: {ex.Message}";
        }
    }

    public event Action? HeartPulseTriggered;

    [RelayCommand]
    public async Task SendHeartPulseAsync()
    {
        if (ConversationId == Guid.Empty) return;

        HeartPulseTriggered?.Invoke();

        var clientMessageId = Guid.NewGuid();
        var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;
        const string pulseText = "نبضة حب بينكما";

        var optimisticItem = new ChatMessageItem
        {
            Id = Guid.Empty,
            ConversationId = ConversationId,
            SenderId = currentUserId,
            SenderUserName = _authState.CurrentUser?.UserName ?? "",
            SenderFullName = _authState.CurrentUser?.FullName ?? "",
            SenderAvatarUrl = _authState.CurrentUser?.AvatarUrl,
            Content = pulseText,
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
                pulseText,
                MessageType.Text,
                clientMessageId);

            await _chatHub.SendMessageAsync(request);
        }
        catch (Exception ex)
        {
            optimisticItem.IsSending = false;
            ErrorMessage = $"تعذر إرسال نبضة الحب: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenCustomizeSheet()
    {
        IsAttachmentPanelVisible = false;
        IsEmojiPickerOpen = false;
        IsCustomizeSheetVisible = true;
    }

    [RelayCommand]
    public void CloseCustomizeSheet()
    {
        IsCustomizeSheetVisible = false;
    }

    [RelayCommand]
    public void SelectWallpaperPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;
        SelectedWallpaperKey = preset;
        CustomWallpaperPath = null;
        ApplyWallpaperPreset(preset);
        SaveCustomizationPreferences();
    }

    [RelayCommand]
    public void SelectBubblePreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;
        SelectedBubbleKey = preset;
        ApplyBubblePreset(preset);
        SaveCustomizationPreferences();
    }

    [RelayCommand]
    public async Task PickChatWallpaperAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "اختر خلفية للمحادثة",
                FileTypes = FilePickerFileType.Images
            });

            if (file == null) return;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var destPath = Path.Combine(FileSystem.AppDataDirectory, $"wallpaper_{ConversationId}{ext}");

            await using (var src = await file.OpenReadAsync())
            await using (var dst = File.Create(destPath))
            {
                await src.CopyToAsync(dst);
            }

            CustomWallpaperPath = destPath;
            SelectedWallpaperKey = "custom";
            SaveCustomizationPreferences();
            IsCustomizeSheetVisible = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر تعيين الخلفية: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ResetChatCustomization()
    {
        CustomWallpaperPath = null;
        SelectedWallpaperKey = "default";
        SelectedBubbleKey = "violet";
        ApplyWallpaperPreset("default");
        ApplyBubblePreset("violet");
        SaveCustomizationPreferences();
    }

    private void LoadChatCustomization(Guid conversationId)
    {
        if (conversationId == Guid.Empty) return;
        try
        {
            var prefix = $"chat_custom_{conversationId}_";
            var wallpaperKey = Preferences.Default.Get(prefix + "wallpaper", "default");
            var bubbleKey = Preferences.Default.Get(prefix + "bubble", "violet");
            var customPath = Preferences.Default.Get(prefix + "wallpaper_path", string.Empty);

            SelectedWallpaperKey = wallpaperKey;
            SelectedBubbleKey = bubbleKey;

            if (wallpaperKey == "custom" && !string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                CustomWallpaperPath = customPath;
            }
            else
            {
                CustomWallpaperPath = null;
                ApplyWallpaperPreset(wallpaperKey);
            }

            ApplyBubblePreset(bubbleKey);
        }
        catch
        {
            // Safe fallback in unit test environment
        }
    }

    private void SaveCustomizationPreferences()
    {
        if (ConversationId == Guid.Empty) return;
        try
        {
            var prefix = $"chat_custom_{ConversationId}_";
            Preferences.Default.Set(prefix + "wallpaper", SelectedWallpaperKey);
            Preferences.Default.Set(prefix + "bubble", SelectedBubbleKey);
            Preferences.Default.Set(prefix + "wallpaper_path", CustomWallpaperPath ?? string.Empty);
        }
        catch
        {
            // Safe fallback in unit test environment
        }
    }

    private void ApplyWallpaperPreset(string preset)
    {
        (ChatBackgroundStartColor, ChatBackgroundEndColor) = preset switch
        {
            "cosmic" => (Color.FromArgb("#14042B"), Color.FromArgb("#2E0854")),
            "rose" => (Color.FromArgb("#280624"), Color.FromArgb("#4A0E3E")),
            "midnight" => (Color.FromArgb("#0A1128"), Color.FromArgb("#1B2845")),
            _ => (Color.FromArgb("#13101A"), Color.FromArgb("#13101A"))
        };
    }

    private void ApplyBubblePreset(string preset)
    {
        (OutgoingBubbleStartColor, OutgoingBubbleEndColor) = preset switch
        {
            "pink" => (Color.FromArgb("#FF4FB8"), Color.FromArgb("#D9268E")),
            "ocean" => (Color.FromArgb("#3B82F6"), Color.FromArgb("#1D4ED8")),
            "emerald" => (Color.FromArgb("#10B981"), Color.FromArgb("#059669")),
            "gold" => (Color.FromArgb("#F59E0B"), Color.FromArgb("#D97706")),
            _ => (Color.FromArgb("#9B3BF9"), Color.FromArgb("#7B2DF3"))
        };
    }

    public async Task InitializeConversationAsync(Guid conversationId)
    {
        LoadChatCustomization(conversationId);
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
            IsEmojiPickerOpen = false;
            _ = LoadStickerPacksAsync();
        }
    }

    [RelayCommand]
    public void CloseAttachmentPanel()
    {
        IsAttachmentPanelVisible = false;
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

        var replyToId = ReplyingToMessage?.Id;
        var replyToContent = ReplyingToMessage?.Content ?? (ReplyingToMessage?.IsImage == true ? "📷 صورة" : ReplyingToMessage?.IsSticker == true ? "🎨 ملصق" : null);
        var replyToSenderName = ReplyingToMessage?.IsMine == true ? "أنت" : ReplyingToMessage?.SenderFullName;
        CancelReply();

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
            ReplyToMessageId = replyToId,
            ReplyToContent = replyToContent,
            ReplyToSenderName = replyToSenderName,
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
                ReplyToMessageId: replyToId,
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
            await UploadAndSendImageAsync(file);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر إرسال الصورة: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CaptureAndSendPhotoAsync()
    {
        IsAttachmentPanelVisible = false;

        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ErrorMessage = "التقاط الصور غير مدعوم على هذا الجهاز.";
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "التقاط صورة"
            });

            if (photo == null) return;
            await UploadAndSendImageAsync(photo);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر التقاط الصورة: {ex.Message}";
        }
    }

    private async Task UploadAndSendImageAsync(FileResult file)
    {
        IsBusy = true;
        try
        {
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

            var replyToId = ReplyingToMessage?.Id;
            var replyToContent = ReplyingToMessage?.Content ?? (ReplyingToMessage?.IsImage == true ? "📷 صورة" : ReplyingToMessage?.IsSticker == true ? "🎨 ملصق" : null);
            var replyToSenderName = ReplyingToMessage?.IsMine == true ? "أنت" : ReplyingToMessage?.SenderFullName;
            CancelReply();

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
                ReplyToMessageId = replyToId,
                ReplyToContent = replyToContent,
                ReplyToSenderName = replyToSenderName,
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
                ReplyToMessageId: replyToId,
                MediaAttachmentId: media.MediaId
            );

            await _chatHub.SendMessageAsync(request);
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
        IsAttachmentPanelVisible = false;
        IsEmojiPickerOpen = false;
        CancelTyping();

        var clientMessageId = Guid.NewGuid();
        var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;

        var replyToId = ReplyingToMessage?.Id;
        var replyToContent = ReplyingToMessage?.Content ?? (ReplyingToMessage?.IsImage == true ? "📷 صورة" : ReplyingToMessage?.IsSticker == true ? "🎨 ملصق" : null);
        var replyToSenderName = ReplyingToMessage?.IsMine == true ? "أنت" : ReplyingToMessage?.SenderFullName;
        CancelReply();

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
            ReplyToMessageId = replyToId,
            ReplyToContent = replyToContent,
            ReplyToSenderName = replyToSenderName,
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
                clientMessageId,
                ReplyToMessageId: replyToId
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
        _chatHub.MessageReactionUpdated += OnMessageReactionUpdated;
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

    private void OnMessageReactionUpdated(MessageReactionUpdateClientDto update)
    {
        if (update.ConversationId != ConversationId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var msg = Messages.FirstOrDefault(m => m.Id == update.MessageId);
            if (msg != null)
            {
                var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;
                bool isMe = update.UserId == currentUserId;

                var clientReactions = update.Reactions.Select(r =>
                {
                    bool hasReacted;
                    if (isMe)
                    {
                        hasReacted = r.HasReacted;
                    }
                    else
                    {
                        var prev = msg.Reactions.FirstOrDefault(p => p.Emoji == r.Emoji);
                        hasReacted = prev?.HasReacted ?? false;
                    }
                    return new ReactionCountClientDto(r.Emoji, r.Count, hasReacted, msg.Id);
                }).ToList();

                msg.Reactions = new ObservableCollection<ReactionCountClientDto>(clientReactions);
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
            _chatHub.MessageReactionUpdated -= OnMessageReactionUpdated;
            _chatHub.UserTyping -= OnUserTyping;
            _chatHub.UserPresenceChanged -= OnUserPresenceChanged;
            _chatHub.ConnectionStateChanged -= OnConnectionStateChanged;
            _typingCts?.Dispose();
            _isDisposed = true;
        }
    }
}
