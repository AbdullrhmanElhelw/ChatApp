using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.Services;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Conversations.ViewModels;

public partial class UserSearchViewModel : BaseViewModel
{
    private readonly IConversationApiService _conversationService;
    private readonly IAuthenticationStateService _authState;
    private readonly INavigationService _navigationService;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<UserProfileDto> SearchResults { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool HasSearched { get; set; }

    [ObservableProperty]
    public partial string SectionTitle { get; set; } = "المستخدمون المقترحون";

    public bool IsEmpty => HasSearched && !IsSearching && SearchResults.Count == 0;

    public UserSearchViewModel(
        IConversationApiService conversationService,
        IAuthenticationStateService authState,
        INavigationService navigationService)
    {
        _conversationService = conversationService;
        _authState = authState;
        _navigationService = navigationService;
        Title = "البحث عن مستخدمين";
    }

    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadInitialUsersAsync();
            return;
        }

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(350, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await SearchAsync();
                });
            }
        }, TaskScheduler.Default);
    }

    [RelayCommand]
    public Task LoadInitialUsersAsync()
    {
        ClearError();
        IsSearching = false;
        HasSearched = false;
        SectionTitle = "ابحث باسم المستخدم الدقيق (@username) أو البريد";
        SearchResults.Clear();
        OnPropertyChanged(nameof(IsEmpty));
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        var rawQuery = SearchQuery?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            await LoadInitialUsersAsync();
            return;
        }

        var query = rawQuery.TrimStart('@');
        ClearError();

        // Check if searching for self
        var current = _authState.CurrentUser;
        if (current != null && (
            string.Equals(query, current.UserName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(query, current.Email, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(query, current.FullName, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "أنت تبحث عن حسابك الحالي! لا يمكن بدء محادثة مع نفسك، ابحث عن اسم مستخدم آخر.";
            SearchResults.Clear();
            HasSearched = true;
            IsSearching = false;
            OnPropertyChanged(nameof(IsEmpty));
            return;
        }

        IsSearching = true;
        HasSearched = true;
        SectionTitle = $"نتائج البحث عن \"{rawQuery}\"";

        try
        {
            var result = await _conversationService.SearchUsersAsync(query);

            if (result.IsSuccess && result.Value != null)
            {
                SearchResults = new ObservableCollection<UserProfileDto>(result.Value);
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
                SearchResults.Clear();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ أثناء البحث: {ex.Message}";
            SearchResults.Clear();
        }
        finally
        {
            IsSearching = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    public async Task StartConversationAsync(UserProfileDto? targetUser)
    {
        if (targetUser == null || IsBusy) return;

        IsBusy = true;
        ClearError();

        try
        {
            var result = await _conversationService.GetOrCreateDirectConversationAsync(targetUser.Id);

            if (result.IsSuccess && result.Value != null)
            {
                var conv = result.Value;
                var parameters = new Dictionary<string, object>
                {
                    { "ConversationId", conv.Id },
                    { "Title", targetUser.FullName },
                    { "AvatarUrl", targetUser.AvatarUrl ?? "" }
                };

                await _navigationService.NavigateToAsync(AppRoutes.Chat, parameters);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "تعذر فتح المحادثة.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateBackAsync()
    {
        await _navigationService.GoBackAsync();
    }
}
