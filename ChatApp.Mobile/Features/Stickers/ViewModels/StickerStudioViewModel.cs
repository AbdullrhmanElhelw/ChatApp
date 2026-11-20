using System.Collections.ObjectModel;
using System.IO.Compression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Navigation;
using Microsoft.Maui.Controls;

namespace ChatApp.Mobile.Features.Stickers.ViewModels;

public partial class StickerStudioViewModel : BaseViewModel
{
    private readonly IStickerApiService _stickerApi;
    private readonly INavigationService _navigationService;
    private readonly IAuthenticationStateService _authState;

    public Func<Task<byte[]?>>? CaptureCanvasAsync { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    public partial string? SelectedImagePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCaption))]
    public partial string CaptionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedColor { get; set; } = "#FFFFFF";

    [ObservableProperty]
    public partial double CaptionFontSize { get; set; } = 22;

    // Shape: "rounded", "circle"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanvasCornerRadius))]
    [NotifyPropertyChangedFor(nameof(IsCircleShape))]
    public partial string SelectedShape { get; set; } = "rounded";

    public bool IsCircleShape => SelectedShape == "circle";
    public CornerRadius CanvasCornerRadius => IsCircleShape ? new CornerRadius(150) : new CornerRadius(28);

    // Border: "none", "white", "neon"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanvasStrokeThickness))]
    [NotifyPropertyChangedFor(nameof(CanvasStrokeColor))]
    public partial string SelectedBorderStyle { get; set; } = "white";

    public double CanvasStrokeThickness => SelectedBorderStyle switch
    {
        "white" => 6,
        "neon" => 4,
        _ => 0
    };

    public Color CanvasStrokeColor => SelectedBorderStyle switch
    {
        "white" => Colors.White,
        "neon" => Color.FromArgb("#FF4FB8"),
        _ => Colors.Transparent
    };

    // Text Position: "top", "center", "bottom"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptionVerticalOptions))]
    public partial string SelectedTextPosition { get; set; } = "bottom";

    public LayoutOptions CaptionVerticalOptions => SelectedTextPosition switch
    {
        "top" => LayoutOptions.Start,
        "center" => LayoutOptions.Center,
        _ => LayoutOptions.End
    };

    // Text Style: "dark", "neon", "outline"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptionBackgroundColor))]
    public partial string SelectedTextStyle { get; set; } = "dark";

    public Color CaptionBackgroundColor => SelectedTextStyle switch
    {
        "neon" => Color.FromArgb("#DDFF4FB8"),
        "outline" => Colors.Transparent,
        _ => Color.FromArgb("#B814042B")
    };

    // Image Fit: true = AspectFill, false = AspectFit
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageAspectMode))]
    [NotifyPropertyChangedFor(nameof(ImageFitLabel))]
    public partial bool IsFillAspect { get; set; } = true;

    public Aspect ImageAspectMode => IsFillAspect ? Aspect.AspectFill : Aspect.AspectFit;
    public string ImageFitLabel => IsFillAspect ? "ملء الإطار" : "الصورة كاملة";

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
        INavigationService navigationService,
        IAuthenticationStateService authState)
    {
        _stickerApi = stickerApi;
        _navigationService = navigationService;
        _authState = authState;
        Title = "صانع الاستيكرات الذكي";
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
                StatusMessage = "تم اختيار الصورة! أضف نصك واختر الشكل والإطار ثم احفظ الاستيكر.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر اختيار الصورة: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SelectColor(string hexColor) => SelectedColor = hexColor;

    [RelayCommand]
    public void SelectShape(string shape) => SelectedShape = shape;

    [RelayCommand]
    public void SelectBorderStyle(string border) => SelectedBorderStyle = border;

    [RelayCommand]
    public void SelectTextPosition(string position) => SelectedTextPosition = position;

    [RelayCommand]
    public void SelectTextStyle(string style) => SelectedTextStyle = style;

    [RelayCommand]
    public void ToggleImageAspect() => IsFillAspect = !IsFillAspect;

    [RelayCommand]
    public void ApplySmartCaption(string caption)
    {
        if (!string.IsNullOrWhiteSpace(caption))
        {
            CaptionText = caption;
        }
    }

    [RelayCommand]
    public void ClearCaption() => CaptionText = string.Empty;

    [RelayCommand]
    public void AddQuickEmoji(string emoji) => CaptionText += emoji;

    [RelayCommand]
    public void IncreaseCaptionSize() => CaptionFontSize = Math.Min(CaptionFontSize + 2, 36);

    [RelayCommand]
    public void DecreaseCaptionSize() => CaptionFontSize = Math.Max(CaptionFontSize - 2, 14);

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
        StatusMessage = "جارٍ تجهيز الاستيكر بالنص والإطار ورفعه...";

        try
        {
            var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;

            // 1. Ensure user has their own custom sticker pack
            var targetPack = StickerPacks.FirstOrDefault(p => p.OwnerId == currentUserId);
            if (targetPack == null)
            {
                var createResult = await _stickerApi.CreatePackAsync("استيكراتي المخصصة 🎨", "استيكرات صنعتها بنفسي", "/uploads/stickers/sparkles.png");
                if (!createResult.IsSuccess || createResult.Value == null)
                {
                    ErrorMessage = createResult.ErrorMessage ?? "فشل إنشاء حزمة الاستيكرات.";
                    return;
                }
                targetPack = createResult.Value;
                StickerPacks.Add(targetPack);
                SelectedPack = targetPack;
            }

            // 2. Capture rendered canvas (image + text + shape + border) as PNG
            byte[]? renderedPng = null;
            if (CaptureCanvasAsync != null)
            {
                renderedPng = await CaptureCanvasAsync();
            }

            ApiResult<UploadMediaResponseClientDto> uploadResult;
            if (renderedPng != null && renderedPng.Length > 0)
            {
                await using var ms = new MemoryStream(renderedPng);
                uploadResult = await _stickerApi.UploadMediaAsync(ms, $"sticker_{Guid.NewGuid():N}.png", "image/png", MessageType.Sticker);
            }
            else
            {
                await using var stream = File.OpenRead(SelectedImagePath);
                var fileName = Path.GetFileName(SelectedImagePath);
                var extension = Path.GetExtension(SelectedImagePath).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    _ => "image/jpeg"
                };
                uploadResult = await _stickerApi.UploadMediaAsync(stream, fileName, contentType, MessageType.Sticker);
            }

            if (!uploadResult.IsSuccess || uploadResult.Value == null)
            {
                ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع صورة الاستيكر.";
                return;
            }

            // 3. Add to pack
            var addResult = await _stickerApi.AddStickerToPackAsync(targetPack.Id, uploadResult.Value.FileUrl);
            if (addResult.IsSuccess)
            {
                StatusMessage = "تم حفظ الاستيكر بالنص والتصميم وأصبح متاحاً في لوحة الملصقات!";
                SelectedImagePath = null;
                CaptionText = string.Empty;
                await Task.Delay(1200);
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
    public async Task ImportWhatsAppStickersAsync()
    {
        try
        {
            var files = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "اختر استيكرات واتساب (.webp / .wastickers / .zip)"
            });

            if (files == null) return;
            var fileList = files.Where(f => f != null).ToList();
            if (fileList.Count == 0) return;

            IsBusy = true;
            ClearError();
            StatusMessage = "جارٍ قراءة وتجهيز استيكرات واتساب...";

            var itemsToUpload = new List<(string FileName, string ContentType, byte[] Data)>();

            foreach (var file in fileList)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is ".zip" or ".wastickers")
                {
                    await using var zipStream = await file.OpenReadAsync();
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(entry.Name) || entry.Length <= 0 || entry.Length > 5 * 1024 * 1024)
                            continue;

                        var entryExt = Path.GetExtension(entry.Name).ToLowerInvariant();
                        var entryMime = GetStickerMimeType(entryExt);
                        if (entryMime == null) continue;

                        await using var es = entry.Open();
                        await using var ms = new MemoryStream();
                        await es.CopyToAsync(ms);
                        itemsToUpload.Add((entry.Name, entryMime, ms.ToArray()));
                    }
                }
                else
                {
                    var mime = GetStickerMimeType(ext);
                    if (mime == null) continue;

                    await using var fs = await file.OpenReadAsync();
                    await using var ms = new MemoryStream();
                    await fs.CopyToAsync(ms);
                    if (ms.Length > 0)
                    {
                        itemsToUpload.Add((file.FileName, mime, ms.ToArray()));
                    }
                }
            }

            if (itemsToUpload.Count == 0)
            {
                ErrorMessage = "لم يتم العثور على صور استيكرات صالحة (.webp / .png / .gif / .wastickers).";
                StatusMessage = string.Empty;
                return;
            }

            var currentUserId = _authState.CurrentUser?.Id ?? Guid.Empty;
            var targetPack = StickerPacks.FirstOrDefault(p => p.OwnerId == currentUserId);
            if (targetPack == null)
            {
                var createResult = await _stickerApi.CreatePackAsync("استيكراتي المخصصة 🎨", "استيكرات صنعتها واستوردتها بنفسي", "/uploads/stickers/sparkles.png");
                if (!createResult.IsSuccess || createResult.Value == null)
                {
                    ErrorMessage = createResult.ErrorMessage ?? "فشل إنشاء حزمة الاستيكرات.";
                    return;
                }
                targetPack = createResult.Value;
                StickerPacks.Add(targetPack);
                SelectedPack = targetPack;
            }

            var uploadedCount = 0;
            foreach (var item in itemsToUpload)
            {
                StatusMessage = $"جارٍ استيراد {uploadedCount + 1} من {itemsToUpload.Count} استيكر...";
                await using var ms = new MemoryStream(item.Data);
                var uploadResult = await _stickerApi.UploadMediaAsync(ms, item.FileName, item.ContentType, MessageType.Sticker);
                if (uploadResult.IsSuccess && uploadResult.Value != null)
                {
                    var addResult = await _stickerApi.AddStickerToPackAsync(targetPack.Id, uploadResult.Value.FileUrl);
                    if (addResult.IsSuccess)
                    {
                        uploadedCount++;
                    }
                }
            }

            StatusMessage = uploadedCount > 0
                ? $"تم استيراد {uploadedCount} استيكر من واتساب بنجاح إلى حزمتك!"
                : "تعذر رفع الاستيكرات المختارة، تأكد من الاتصال بالخادم.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر استيراد الاستيكرات: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string? GetStickerMimeType(string ext) => ext switch
    {
        ".webp" => "image/webp",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => null
    };

    [RelayCommand]
    public async Task NavigateBackAsync() => await _navigationService.GoBackAsync();
}
