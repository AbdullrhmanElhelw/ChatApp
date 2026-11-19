using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
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

    public bool IsEmpty => FilteredConversations.Count == 0 && !IsBusy;

    public ConversationsViewModel(
        IConversationApiService conversationService,
        IAuthenticationStateService authStateService,
        INavigationService navigationService)
    {
        _conversationService = conversationService;
        _authStateService = authStateService;
        _navigationService = navigationService;

        Title = "المحادثات";
        CurrentUser = _authStateService.CurrentUser;
        UpdateThemeIcon();
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
    private void ToggleTheme()
    {
        ThemeManager.ToggleTheme();
        UpdateThemeIcon();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _authStateService.SetUnauthenticated();
        await _navigationService.NavigateToAsync(AppRoutes.Welcome);
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
