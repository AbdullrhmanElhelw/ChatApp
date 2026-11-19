using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Authentication.ViewModels;

public partial class ProfileSetupViewModel : BaseViewModel
{
    private readonly IAuthApiService _authService;
    private readonly IStickerApiService _stickerApi;
    private readonly IAuthenticationStateService _authStateService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Bio { get; set; } = "أستخدم تطبيق BetweenUs للمراسلة الخاصة! 💜";

    [ObservableProperty]
    public partial string? AvatarUrl { get; set; }

    public ProfileSetupViewModel(
        IAuthApiService authService,
        IStickerApiService stickerApi,
        IAuthenticationStateService authStateService,
        INavigationService navigationService)
    {
        _authService = authService;
        _stickerApi = stickerApi;
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
            AvatarUrl = user.AvatarUrl;
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

        ClearError();
        var currentUser = _authStateService.CurrentUser;
        var finalName = string.IsNullOrWhiteSpace(DisplayName)
            ? (currentUser?.FullName ?? "مستخدم")
            : DisplayName.Trim();

        if (finalName.Length < 2)
        {
            ErrorMessage = "اسم العرض يجب أن يكون حرفين على الأقل.";
            return;
        }

        IsBusy = true;
        try
        {
            var request = new UpdateProfileRequestDto(
                finalName,
                string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim(),
                AvatarUrl
            );

            var result = await _authService.UpdateProfileAsync(request);
            if (result.IsSuccess && result.Value != null)
            {
                _authStateService.UpdateCurrentUser(result.Value);
                await _navigationService.NavigateToAsync(AppRoutes.MainConversations);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "تعذر حفظ الملف الشخصي.";
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
    private async Task PickAvatarAsync()
    {
        if (IsBusy) return;

        ClearError();
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "اختر صورة ملفك الشخصي",
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
            if (uploadResult.IsSuccess && uploadResult.Value != null)
            {
                AvatarUrl = uploadResult.Value.FileUrl;
            }
            else
            {
                ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع الصورة الشخصية.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر اختيار الصورة: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
