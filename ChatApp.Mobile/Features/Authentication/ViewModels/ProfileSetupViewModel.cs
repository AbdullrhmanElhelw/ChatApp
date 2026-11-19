using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public partial class ProfileSetupViewModel : BaseViewModel
{
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Bio { get; set; } = "أستخدم تطبيق ChatApp للمراسلة الفورية! 🚀";

    [ObservableProperty]
    public partial string? AvatarUrl { get; set; }

    public ProfileSetupViewModel(
        IAuthenticationStateService authStateService,
        INavigationService navigationService)
    {
        _authStateService = authStateService;
        _navigationService = navigationService;
        Title = "إعداد الملف الشخصي";

        InitializeUserData();
    }

    private void InitializeUserData()
    {
        var user = _authStateService.CurrentUser;
        if (user != null)
        {
            DisplayName = user.FullName;
            UserName = user.UserName;
            if (!string.IsNullOrEmpty(user.Bio))
            {
                Bio = user.Bio;
            }
        }
    }

    [RelayCommand]
    private async Task CompleteSetupAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var currentUser = _authStateService.CurrentUser;
            if (currentUser != null)
            {
                var updatedUser = new UserProfileDto(
                    currentUser.Id,
                    currentUser.UserName,
                    currentUser.Email,
                    string.IsNullOrWhiteSpace(DisplayName) ? currentUser.FullName : DisplayName.Trim(),
                    Bio.Trim(),
                    AvatarUrl,
                    true,
                    DateTime.UtcNow,
                    currentUser.CreatedAt
                );

                _authStateService.UpdateCurrentUser(updatedUser);
            }

            // Finish onboarding and enter main conversations
            await _navigationService.NavigateToAsync(AppRoutes.MainConversations);
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
    private async Task PickAvatarAsync()
    {
        // Placeholder for media picker or preset avatars
        await Task.CompletedTask;
    }
}
