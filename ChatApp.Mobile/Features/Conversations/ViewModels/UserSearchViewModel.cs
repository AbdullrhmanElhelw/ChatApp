using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.Services;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Conversations.ViewModels;

public partial class UserSearchViewModel : BaseViewModel
{
    private readonly IConversationApiService _conversationService;
    private readonly INavigationService _navigationService;

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

    public bool IsEmpty => HasSearched && !IsSearching && SearchResults.Count == 0;

    public UserSearchViewModel(
        IConversationApiService conversationService,
        INavigationService navigationService)
    {
        _conversationService = conversationService;
        _navigationService = navigationService;
        Title = "البحث عن مستخدمين";
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            HasSearched = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        HasSearched = true;
        ClearError();

        try
        {
            var result = await _conversationService.SearchUsersAsync(SearchQuery);

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
