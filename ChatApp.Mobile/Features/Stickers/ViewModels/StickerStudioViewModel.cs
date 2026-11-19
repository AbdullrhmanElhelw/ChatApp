using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.Stickers.ViewModels;

public partial class StickerStudioViewModel : BaseViewModel
{
    private readonly IStickerApiService _stickerApi;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    public partial string? SelectedImagePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCaption))]
    public partial string CaptionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedColor { get; set; } = "#FFFFFF";

    [ObservableProperty]
    public partial ObservableCollection<StickerPackClientDto> StickerPacks { get; set; } = new();

    [ObservableProperty]
    public partial StickerPackClientDto? SelectedPack { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool HasImage => !string.IsNullOrEmpty(SelectedImagePath);
    public bool HasCaption => !string.IsNullOrWhiteSpace(CaptionText);
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public StickerStudioViewModel(
        IStickerApiService stickerApi,
        INavigationService navigationService)
    {
        _stickerApi = stickerApi;
        _navigationService = navigationService;
        Title = "صانع الاستيكرات (Sticker Studio)";
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _stickerApi.GetMyPacksAsync();
            if (result.IsSuccess && result.Value != null)
            {
                StickerPacks = new ObservableCollection<StickerPackClientDto>(result.Value);
                SelectedPack = StickerPacks.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر تحميل الحزم: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task PickImageAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "اختر صورة لصنع استيكر",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                SelectedImagePath = result.FullPath;
                ClearError();
                StatusMessage = "تم اختيار الصورة بنجاح! يمكنك الآن كتابة نص أو حفظ الاستيكر.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر اختيار الصورة: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SelectColor(string hexColor)
    {
        SelectedColor = hexColor;
    }

    [RelayCommand]
    public void AddQuickEmoji(string emoji)
    {
        CaptionText += emoji;
    }

    [RelayCommand]
    public async Task SaveStickerAsync()
    {
        if (string.IsNullOrEmpty(SelectedImagePath) || !File.Exists(SelectedImagePath))
        {
            ErrorMessage = "يرجى اختيار صورة أولاً لصنع الاستيكر.";
            return;
        }

        IsBusy = true;
        ClearError();
        StatusMessage = "جارٍ حفظ وتحويل الاستيكر...";

        try
        {
            // 1. Ensure user has a sticker pack
            var targetPack = SelectedPack;
            if (targetPack == null)
            {
                var createResult = await _stickerApi.CreatePackAsync("استيكراتي المخصصة 🎨", "استيكرات صنعتها بنفسي", "https://api.iconify.design/noto:artist-palette.svg");
                if (!createResult.IsSuccess || createResult.Value == null)
                {
                    ErrorMessage = createResult.ErrorMessage ?? "فشل إنشاء حزمة الاستيكرات.";
                    return;
                }
                targetPack = createResult.Value;
                StickerPacks.Add(targetPack);
                SelectedPack = targetPack;
            }

            // 2. Upload file
            await using var stream = File.OpenRead(SelectedImagePath);
            var fileName = Path.GetFileName(SelectedImagePath);
            var extension = Path.GetExtension(SelectedImagePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            var uploadResult = await _stickerApi.UploadMediaAsync(stream, fileName, contentType, MessageType.Sticker);
            if (!uploadResult.IsSuccess || uploadResult.Value == null)
            {
                ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع صورة الاستيكر.";
                return;
            }

            // 3. Add to pack
            var addResult = await _stickerApi.AddStickerToPackAsync(targetPack.Id, uploadResult.Value.FileUrl);
            if (addResult.IsSuccess)
            {
                StatusMessage = "🎉 تم حفظ الاستيكر بنجاح وأصبح متاحاً في لوحة الملصقات بالشات!";
                SelectedImagePath = null;
                CaptionText = string.Empty;
                await Task.Delay(1500);
                await _navigationService.GoBackAsync();
            }
            else
            {
                ErrorMessage = addResult.ErrorMessage ?? "فشل ربط الاستيكر بالحزمة.";
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
    public async Task NavigateBackAsync()
    {
        await _navigationService.GoBackAsync();
    }
}
