using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Features.Conversations.Services;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Conversations.ViewModels;

public enum ConversationFilter
{
    All,
    Unread,
    Groups
}

public partial class ConversationsViewModel : BaseViewModel
{
    private readonly IConversationApiService _conversationService;
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;
    private readonly IChatHubService? _chatHubService;
    private readonly IAuthApiService? _authService;

    [ObservableProperty]
    public partial ObservableCollection<ConversationSummaryDto> Conversations { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<ConversationSummaryDto> FilteredConversations { get; set; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ConversationFilter SelectedFilter { get; set; } = ConversationFilter.All;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial UserProfileDto? CurrentUser { get; set; }

    [ObservableProperty]
    public partial string CurrentThemeIcon { get; set; } = "🌓";

    [ObservableProperty]
    public partial string CurrentMoodLabel { get; set; } = "عالمكم الصغير";

    [ObservableProperty]
    public partial bool IsMoodSheetVisible { get; set; }

    public bool IsEmpty => FilteredConversations.Count == 0 && !IsBusy;

    public ConversationsViewModel(
        IConversationApiService conversationService,
        IAuthenticationStateService authStateService,
        INavigationService navigationService,
        IChatHubService? chatHubService = null,
        IAuthApiService? authService = null)
    {
        _conversationService = conversationService;
        _authStateService = authStateService;
        _navigationService = navigationService;
        _chatHubService = chatHubService;
        _authService = authService;

        Title = "المحادثات";
        CurrentUser = _authStateService.CurrentUser;
        UpdateThemeIcon();

        if (_chatHubService != null)
        {
            _chatHubService.MessageReceived += OnRealtimeMessageReceived;
            _chatHubService.UserPresenceChanged += OnUserPresenceChanged;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterConversations();
    }

    partial void OnSelectedFilterChanged(ConversationFilter value)
    {
        FilterConversations();
    }

    [RelayCommand]
    private void SelectFilter(ConversationFilter filter)
    {
        SelectedFilter = filter;
    }

    [RelayCommand]
    public async Task LoadConversationsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ClearError();

        try
        {
            CurrentUser = _authStateService.CurrentUser;
            var result = await _conversationService.GetConversationsAsync();

            if (result.IsSuccess && result.Value != null)
            {
                Conversations = new ObservableCollection<ConversationSummaryDto>(result.Value);
                FilterConversations();
                _ = EnsureHubConnectedAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر جلب المحادثات: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadConversationsAsync();
    }

    [RelayCommand]
    private async Task SelectConversationAsync(ConversationSummaryDto? conversation)
    {
        if (conversation == null) return;

        var parameters = new Dictionary<string, object>
        {
            { "ConversationId", conversation.Id },
            { "Title", conversation.Title },
            { "AvatarUrl", conversation.AvatarUrl ?? "" }
        };

        // In Phase 7, Chat Page will consume this route
        await _navigationService.NavigateToAsync(AppRoutes.Chat, parameters);
    }

    [RelayCommand]
    private async Task OpenUserSearchAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.UserSearch);
    }

    [RelayCommand]
    private async Task OpenProfileSetupAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.ProfileSetup);
    }

    [RelayCommand]
    private async Task OpenCoupleSpaceAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.CoupleSpace);
    }

    [RelayCommand]
    private async Task OpenSettingsMenuAsync()
    {
        await _navigationService.NavigateToAsync(AppRoutes.Settings);
    }

    [RelayCommand]
    private void OpenMoodCheckIn()
    {
        IsMoodSheetVisible = true;
    }

    [RelayCommand]
    private void CloseMoodSheet()
    {
        IsMoodSheetVisible = false;
    }

    [RelayCommand]
    private void SelectMood(string? mood)
    {
        if (!string.IsNullOrWhiteSpace(mood))
        {
            CurrentMoodLabel = mood;
            Preferences.Default.Set("betweenus_mood", mood);
        }
        IsMoodSheetVisible = false;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeManager.ToggleTheme();
        UpdateThemeIcon();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            if (_chatHubService != null)
            {
                await _chatHubService.DisconnectAsync();
            }

            if (_authService != null)
            {
                await _authService.LogoutAsync();
            }
        }
        catch
        {
            // Proceed with local sign-out even if network revocation fails
        }

        _authStateService.SetUnauthenticated();
        await _navigationService.NavigateToAsync(AppRoutes.Welcome);
    }

    private async Task EnsureHubConnectedAsync()
    {
        if (_chatHubService == null || _chatHubService.IsConnected) return;
        try
        {
            await _chatHubService.ConnectAsync();
        }
        catch
        {
            // Non-fatal: list already loaded via REST
        }
    }

    private void OnRealtimeMessageReceived(MessageClientDto message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var index = -1;
            for (var i = 0; i < Conversations.Count; i++)
            {
                if (Conversations[i].Id == message.ConversationId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                _ = LoadConversationsAsync();
                return;
            }

            var existing = Conversations[index];
            var isMine = CurrentUser != null && message.SenderId == CurrentUser.Id;
            var updated = existing with
            {
                LastMessage = new LastMessageDto(
                    message.Id,
                    message.Content,
                    message.MessageType,
                    message.SentAt,
                    message.SenderId,
                    message.SenderFullName),
                LastActivityAt = message.SentAt,
                UnreadCount = isMine ? existing.UnreadCount : existing.UnreadCount + 1
            };

            Conversations.RemoveAt(index);
            Conversations.Insert(0, updated);
            FilterConversations();
        });
    }

    private void OnUserPresenceChanged(Guid userId, bool isOnline, DateTime? lastSeenAt)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var changed = false;
            for (var i = 0; i < Conversations.Count; i++)
            {
                if (Conversations[i].OtherParticipantId == userId)
                {
                    Conversations[i] = Conversations[i] with
                    {
                        IsOtherParticipantOnline = isOnline,
                        OtherParticipantLastSeenAt = lastSeenAt
                    };
                    changed = true;
                }
            }

            if (changed)
            {
                FilterConversations();
            }
        });
    }

    private void FilterConversations()
    {
        IEnumerable<ConversationSummaryDto> matches = Conversations;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            matches = matches.Where(c =>
                c.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                (c.LastMessage?.Content?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false));
        }

        matches = SelectedFilter switch
        {
            ConversationFilter.Unread => matches.Where(c => c.UnreadCount > 0),
            ConversationFilter.Groups => matches.Where(c => c.ConversationType == ConversationType.Group),
            _ => matches
        };

        FilteredConversations = new ObservableCollection<ConversationSummaryDto>(matches);
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void UpdateThemeIcon()
    {
        CurrentThemeIcon = ThemeManager.IsDarkMode ? "☀️" : "🌙";
    }
}
