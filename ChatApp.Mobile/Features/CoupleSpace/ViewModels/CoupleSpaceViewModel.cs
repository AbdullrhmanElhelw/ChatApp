using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;
using ChatApp.Mobile.Features.Conversations.Services;
using ChatApp.Mobile.Features.CoupleSpace.DTOs;
using ChatApp.Mobile.Features.CoupleSpace.Models;
using ChatApp.Mobile.Features.CoupleSpace.Services;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.CoupleSpace.ViewModels;

public enum CoupleTab
{
    Home,
    Dates,
    Memories,
    LoveNotes
}

public partial class CoupleSpaceViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly ICoupleApiService? _coupleApi;
    private readonly IConversationApiService? _conversationApi;
    private readonly IStickerApiService? _stickerApi;
    private static CoupleSpaceClientDto? _cachedSpaceDto;

    private static readonly string[] DailyQuestions =
    [
        "إيه أكتر موقف ضحّكك مع بعض الأسبوع ده؟",
        "لو نسافر بكره لأي مكان سوا، تحب نروح فين؟",
        "إيه أكتر تفصيلة صغيرة في شريكك بتفرحك دايماً؟",
        "أول انطباع أخدته عن بعض كان إيه؟",
        "إيه أكتر أغنية بتفكّرك بلحظاتنا الحلوة؟",
        "حلم نفسي نحققه سوا خلال السنة الجاية؟",
        "إيه أجمل ذكرى قضيناها مع بعض لحد النهارده؟"
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeTab))]
    [NotifyPropertyChangedFor(nameof(IsDatesTab))]
    [NotifyPropertyChangedFor(nameof(IsMemoriesTab))]
    [NotifyPropertyChangedFor(nameof(IsLoveNotesTab))]
    public partial CoupleTab ActiveTab { get; set; } = CoupleTab.Home;

    public bool IsHomeTab => ActiveTab == CoupleTab.Home;
    public bool IsDatesTab => ActiveTab == CoupleTab.Dates;
    public bool IsMemoriesTab => ActiveTab == CoupleTab.Memories;
    public bool IsLoveNotesTab => ActiveTab == CoupleTab.LoveNotes;

    // Couple Pairing State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPaired))]
    public partial bool IsPaired { get; set; }

    public bool IsNotPaired => !IsPaired;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInviteCode))]
    [NotifyPropertyChangedFor(nameof(NeedsInviteGeneration))]
    public partial string InviteCode { get; set; } = string.Empty;

    public bool HasInviteCode => !string.IsNullOrWhiteSpace(InviteCode);
    public bool NeedsInviteGeneration => !HasInviteCode;

    [ObservableProperty]
    public partial string JoinCodeInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Guid? PartnerId { get; set; }

    [ObservableProperty]
    public partial string PartnerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PartnerUserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? PartnerAvatarUrl { get; set; }

    [ObservableProperty]
    public partial bool IsPartnerOnline { get; set; }

    [ObservableProperty]
    public partial string PartnerMood { get; set; } = "عالمكم الصغير";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPairingFeedback))]
    public partial string? PairingFeedback { get; set; }

    public bool HasPairingFeedback => !string.IsNullOrWhiteSpace(PairingFeedback);

    // Daily Question & Lists
    [ObservableProperty]
    public partial string TodayQuestion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TodayAnswer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAnswerSaved { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SpecialDateItem> SpecialDates { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<MemoryItem> Memories { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMemoryDetailVisible))]
    public partial MemoryItem? SelectedMemory { get; set; }

    public bool IsMemoryDetailVisible => SelectedMemory != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMemoryImageViewerVisible))]
    public partial string? PreviewMemoryImageUrl { get; set; }

    public bool IsMemoryImageViewerVisible => !string.IsNullOrWhiteSpace(PreviewMemoryImageUrl);

    [RelayCommand]
    private void OpenMemoryImagePreview(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        PreviewMemoryImageUrl = url;
    }

    [RelayCommand]
    private void CloseMemoryImagePreview() => PreviewMemoryImageUrl = null;

    [ObservableProperty]
    public partial ObservableCollection<LoveNoteItem> LoveNotes { get; set; } = new();

    // Modals
    [ObservableProperty]
    public partial bool IsAddDateSheetVisible { get; set; }

    [ObservableProperty]
    public partial string NewDateTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime NewDateValue { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial bool IsAddMemorySheetVisible { get; set; }

    [ObservableProperty]
    public partial string NewMemoryTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMemoryNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMemoryLocation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime NewMemoryDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string? NewMemoryImagePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMemoryPhotosLabel))]
    public partial List<string> NewMemoryImagePaths { get; set; } = new();

    public string SelectedMemoryPhotosLabel => NewMemoryImagePaths.Count > 0
        ? $"تم اختيار {NewMemoryImagePaths.Count} صور للذكرى"
        : "اختيار صورة أو أكثر للذكرى";

    [ObservableProperty]
    public partial bool IsAddMomentSheetVisible { get; set; }

    [ObservableProperty]
    public partial string NewMomentTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMomentCaption { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime NewMomentDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial TimeSpan NewMomentTime { get; set; } = DateTime.Now.TimeOfDay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMomentPhoto))]
    [NotifyPropertyChangedFor(nameof(SelectedMomentPhotoLabel))]
    public partial string? NewMomentImagePath { get; set; }

    public bool HasSelectedMomentPhoto => !string.IsNullOrWhiteSpace(NewMomentImagePath);
    public string SelectedMomentPhotoLabel => HasSelectedMomentPhoto ? "تم اختيار صورة اللحظة" : "اختيار صورة للحظة";

    [ObservableProperty]
    public partial bool IsAddNoteSheetVisible { get; set; }

    [ObservableProperty]
    public partial string NewNoteTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewNoteMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime NewNoteUnlockDate { get; set; } = DateTime.Today.AddDays(7);

    public CoupleSpaceViewModel(
        INavigationService navigationService,
        ICoupleApiService? coupleApi = null,
        IConversationApiService? conversationApi = null,
        IStickerApiService? stickerApi = null)
    {
        _navigationService = navigationService;
        _coupleApi = coupleApi;
        _conversationApi = conversationApi;
        _stickerApi = stickerApi;
        Title = "عالمنا الخاص";
        LoadLocal();
    }

    public async Task LoadFromServerAsync()
    {
        if (_cachedSpaceDto != null)
        {
            ApplyServerSpace(_cachedSpaceDto);
        }

        if (_coupleApi == null) return;
        try
        {
            var res = await _coupleApi.GetMySpaceAsync();
            if (res.IsSuccess && res.Value != null)
            {
                _cachedSpaceDto = res.Value;
                ApplyServerSpace(res.Value);
            }
        }
        catch { }
    }

    private void ApplyServerSpace(CoupleSpaceClientDto dto)
    {
        InviteCode = dto.InviteCode;
        IsPaired = dto.IsPaired;

        if (dto.Partner != null)
        {
            PartnerId = dto.Partner.Id;
            PartnerName = dto.Partner.FullName;
            PartnerUserName = dto.Partner.UserName;
            PartnerAvatarUrl = dto.Partner.AvatarUrl;
            IsPartnerOnline = dto.Partner.IsOnline;
            PartnerMood = !string.IsNullOrWhiteSpace(dto.Partner.Mood) ? dto.Partner.Mood : "عالمكم الصغير";
        }
        else
        {
            PartnerId = null;
            PartnerName = string.Empty;
            PartnerUserName = string.Empty;
            PartnerAvatarUrl = null;
            IsPartnerOnline = false;
        }

        if (!string.IsNullOrWhiteSpace(dto.SpecialDatesJson))
        {
            try
            {
                var dates = JsonSerializer.Deserialize<List<SpecialDateItem>>(dto.SpecialDatesJson);
                if (dates != null) SpecialDates = new ObservableCollection<SpecialDateItem>(dates);
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(dto.MemoriesJson))
        {
            try
            {
                var mems = JsonSerializer.Deserialize<List<MemoryItem>>(dto.MemoriesJson);
                if (mems != null) Memories = new ObservableCollection<MemoryItem>(mems);
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(dto.LoveNotesJson))
        {
            try
            {
                var notes = JsonSerializer.Deserialize<List<LoveNoteItem>>(dto.LoveNotesJson);
                if (notes != null) LoveNotes = new ObservableCollection<LoveNoteItem>(notes);
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(dto.DailyAnswersJson))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(dto.DailyAnswersJson);
                var key = $"{DateTime.Today:yyyyMMdd}";
                if (dict != null && dict.TryGetValue(key, out var ans))
                {
                    TodayAnswer = ans;
                    IsAnswerSaved = !string.IsNullOrWhiteSpace(ans);
                }
            }
            catch { }
        }
    }

    private void LoadLocal()
    {
        var dayIndex = DateTime.Today.DayOfYear % DailyQuestions.Length;
        TodayQuestion = DailyQuestions[dayIndex];

        try
        {
            var answerKey = $"couple_answer_{DateTime.Today:yyyyMMdd}";
            TodayAnswer = Preferences.Default.Get(answerKey, string.Empty);
            IsAnswerSaved = !string.IsNullOrWhiteSpace(TodayAnswer);

            var datesJson = Preferences.Default.Get("couple_special_dates", string.Empty);
            if (!string.IsNullOrWhiteSpace(datesJson))
            {
                var loaded = JsonSerializer.Deserialize<List<SpecialDateItem>>(datesJson);
                if (loaded != null) SpecialDates = new ObservableCollection<SpecialDateItem>(loaded);
            }
            else
            {
                SpecialDates = new ObservableCollection<SpecialDateItem>
                {
                    new(Guid.NewGuid(), "أول رسالة بيننا", DateTime.Today.AddMonths(-6)),
                    new(Guid.NewGuid(), "أول مقابلة", DateTime.Today.AddMonths(-4)),
                    new(Guid.NewGuid(), "ذكرى ارتباطنا", DateTime.Today.AddDays(30))
                };
            }

            var memJson = Preferences.Default.Get("couple_memories", string.Empty);
            if (!string.IsNullOrWhiteSpace(memJson))
            {
                var loaded = JsonSerializer.Deserialize<List<MemoryItem>>(memJson);
                if (loaded != null) Memories = new ObservableCollection<MemoryItem>(loaded);
            }

            var notesJson = Preferences.Default.Get("couple_love_notes", string.Empty);
            if (!string.IsNullOrWhiteSpace(notesJson))
            {
                var loaded = JsonSerializer.Deserialize<List<LoveNoteItem>>(notesJson);
                if (loaded != null) LoveNotes = new ObservableCollection<LoveNoteItem>(loaded);
            }
            else
            {
                LoveNotes = new ObservableCollection<LoveNoteItem>
                {
                    new(Guid.NewGuid(), "رسالة مفاجأة", "وجودك في حياتي هو أجمل حاجة حصلتلي، كل يوم معاكي عالم تاني.", DateTime.Today, true),
                    new(Guid.NewGuid(), "رسالة للذكرى الجاية", "رسالة خاصة هتتفتح يوم مناسبتنا الجاية!", DateTime.Today.AddDays(14), false)
                };
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task CreateInviteCodeAsync()
    {
        if (_coupleApi == null || IsBusy) return;
        IsBusy = true;
        PairingFeedback = null;
        try
        {
            var res = await _coupleApi.CreateSpaceAsync();
            if (res.IsSuccess && res.Value != null)
            {
                ApplyServerSpace(res.Value);
                PairingFeedback = "تم إنشاء كود الدعوة! ابعته لشريكك ليرتبط بحسابك.";
            }
            else
            {
                PairingFeedback = res.ErrorMessage ?? "تعذر إنشاء كود الدعوة.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyInviteCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(InviteCode)) return;
        try
        {
            await Clipboard.Default.SetTextAsync(InviteCode);
            PairingFeedback = $"تم نسخ الكود ({InviteCode})! ابعته لشريكك.";
        }
        catch { }
    }

    [RelayCommand]
    private async Task JoinByCodeAsync()
    {
        if (_coupleApi == null || IsBusy) return;
        if (string.IsNullOrWhiteSpace(JoinCodeInput))
        {
            PairingFeedback = "من فضلك أدخل كود الدعوة أولاً.";
            return;
        }

        IsBusy = true;
        PairingFeedback = null;
        try
        {
            var res = await _coupleApi.JoinByCodeAsync(JoinCodeInput.Trim());
            if (res.IsSuccess && res.Value != null)
            {
                ApplyServerSpace(res.Value);
                JoinCodeInput = string.Empty;
                PairingFeedback = "تم ربط حسابكما بنجاح!";
            }
            else
            {
                PairingFeedback = res.ErrorMessage ?? "كود الدعوة غير صحيح.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UnlinkCoupleAsync()
    {
        if (_coupleApi == null || IsBusy) return;
        IsBusy = true;
        try
        {
            var res = await _coupleApi.UnlinkAsync();
            if (res.IsSuccess)
            {
                IsPaired = false;
                InviteCode = string.Empty;
                PartnerName = string.Empty;
                PartnerUserName = string.Empty;
                PartnerAvatarUrl = null;
                PairingFeedback = "تم فك الربط.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPartnerChatAsync()
    {
        if (_conversationApi == null || PartnerId == null || PartnerId == Guid.Empty) return;
        IsBusy = true;
        try
        {
            var res = await _conversationApi.GetOrCreateDirectConversationAsync(PartnerId.Value);
            if (res.IsSuccess && res.Value != null)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "ConversationId", res.Value.Id },
                    { "Title", PartnerName },
                    { "AvatarUrl", PartnerAvatarUrl ?? string.Empty }
                };
                await _navigationService.NavigateToAsync(AppRoutes.Chat, parameters);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateBackAsync() => await _navigationService.GoBackAsync();

    [RelayCommand]
    private void SelectTab(CoupleTab tab) => ActiveTab = tab;

    [RelayCommand]
    private void SaveDailyAnswer()
    {
        if (string.IsNullOrWhiteSpace(TodayAnswer)) return;
        try
        {
            Preferences.Default.Set($"couple_answer_{DateTime.Today:yyyyMMdd}", TodayAnswer.Trim());
            IsAnswerSaved = true;
        }
        catch { }
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void OpenAddDateSheet()
    {
        NewDateTitle = string.Empty;
        NewDateValue = DateTime.Today;
        IsAddDateSheetVisible = true;
    }

    [RelayCommand]
    private void CloseAllSheets()
    {
        IsAddDateSheetVisible = false;
        IsAddMemorySheetVisible = false;
        IsAddMomentSheetVisible = false;
        IsAddNoteSheetVisible = false;
    }

    [RelayCommand]
    private void SaveSpecialDate()
    {
        if (string.IsNullOrWhiteSpace(NewDateTitle)) return;
        SpecialDates.Insert(0, new SpecialDateItem(Guid.NewGuid(), NewDateTitle.Trim(), NewDateValue));
        SaveList("couple_special_dates", SpecialDates);
        IsAddDateSheetVisible = false;
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void DeleteSpecialDate(SpecialDateItem? item)
    {
        if (item == null) return;
        SpecialDates.Remove(item);
        SaveList("couple_special_dates", SpecialDates);
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void OpenAddMemorySheet()
    {
        NewMemoryTitle = string.Empty;
        NewMemoryNote = string.Empty;
        NewMemoryLocation = string.Empty;
        NewMemoryDate = DateTime.Today;
        NewMemoryImagePath = null;
        NewMemoryImagePaths = new List<string>();
        OnPropertyChanged(nameof(SelectedMemoryPhotosLabel));
        IsAddMemorySheetVisible = true;
    }

    [RelayCommand]
    private async Task PickMemoryPhotoAsync()
    {
        try
        {
            var files = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "اختر صورة أو أكثر للذكرى",
                FileTypes = FilePickerFileType.Images
            });
            if (files == null) return;

            var savedPaths = new List<string>(NewMemoryImagePaths);
            foreach (var file in files)
            {
                if (file == null) continue;
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                var dest = Path.Combine(FileSystem.AppDataDirectory, $"memory_{Guid.NewGuid():N}{ext}");
                await using var src = await file.OpenReadAsync();
                await using var dst = File.Create(dest);
                await src.CopyToAsync(dst);
                savedPaths.Add(dest);
            }

            NewMemoryImagePaths = savedPaths;
            NewMemoryImagePath = savedPaths.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedMemoryPhotosLabel));
        }
        catch { }
    }

    [RelayCommand]
    private void SaveMemory()
    {
        if (string.IsNullOrWhiteSpace(NewMemoryTitle)) return;
        var note = NewMemoryNote.Trim();
        var paths = NewMemoryImagePaths.ToList();
        var moments = paths
            .Select((path, index) => new MemoryMoment(
                Guid.NewGuid(),
                index == 0 && !string.IsNullOrWhiteSpace(note) ? note : null,
                NewMemoryDate,
                path))
            .ToList();

        Memories.Insert(0, new MemoryItem(
            Guid.NewGuid(),
            NewMemoryTitle.Trim(),
            note,
            string.IsNullOrWhiteSpace(NewMemoryLocation) ? null : NewMemoryLocation.Trim(),
            NewMemoryDate,
            NewMemoryImagePath,
            paths,
            moments));
        SaveList("couple_memories", Memories);
        IsAddMemorySheetVisible = false;
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void OpenAddMomentSheet()
    {
        if (SelectedMemory == null) return;
        NewMomentTitle = string.Empty;
        NewMomentCaption = string.Empty;
        NewMomentDate = SelectedMemory.MemoryDate;
        NewMomentTime = DateTime.Now.TimeOfDay;
        NewMomentImagePath = null;
        IsAddMomentSheetVisible = true;
    }

    [RelayCommand]
    private async Task PickMomentPhotoAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "اختر صورة للحظة",
                FileTypes = FilePickerFileType.Images
            });
            if (file == null) return;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var dest = Path.Combine(FileSystem.AppDataDirectory, $"moment_{Guid.NewGuid():N}{ext}");
            await using var src = await file.OpenReadAsync();
            await using var dst = File.Create(dest);
            await src.CopyToAsync(dst);
            NewMomentImagePath = dest;
        }
        catch { }
    }

    [RelayCommand]
    private void SaveMoment()
    {
        if (SelectedMemory == null) return;
        if (string.IsNullOrWhiteSpace(NewMomentTitle) && string.IsNullOrWhiteSpace(NewMomentImagePath)) return;

        var moment = new MemoryMoment(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(NewMomentCaption) ? null : NewMomentCaption.Trim(),
            NewMomentDate,
            NewMomentImagePath ?? string.Empty,
            string.IsNullOrWhiteSpace(NewMomentTitle) ? null : NewMomentTitle.Trim(),
            NewMomentTime);

        var moments = new List<MemoryMoment>(SelectedMemory.DisplayMoments);
        moments.Add(moment);

        var paths = new List<string>(SelectedMemory.DisplayImages);
        if (!string.IsNullOrWhiteSpace(NewMomentImagePath))
        {
            paths.Add(NewMomentImagePath);
        }

        var updated = SelectedMemory with
        {
            ImagePath = SelectedMemory.ImagePath ?? paths.FirstOrDefault(),
            ImagePaths = paths,
            Moments = moments
        };

        var idx = Memories.IndexOf(SelectedMemory);
        if (idx >= 0)
        {
            Memories[idx] = updated;
        }
        SelectedMemory = updated;
        SaveList("couple_memories", Memories);
        IsAddMomentSheetVisible = false;
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void OpenMemory(MemoryItem? item)
    {
        if (item == null) return;
        SelectedMemory = item;
    }

    [RelayCommand]
    private void CloseMemoryDetail()
    {
        SelectedMemory = null;
    }

    [RelayCommand]
    private async Task AddPhotosToSelectedMemoryAsync()
    {
        if (SelectedMemory == null) return;
        try
        {
            var files = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "أضف صوراً جديدة لهذه الذكرى",
                FileTypes = FilePickerFileType.Images
            });
            if (files == null) return;

            var newMoments = new List<MemoryMoment>(SelectedMemory.DisplayMoments);
            var newPaths = new List<string>(SelectedMemory.DisplayImages);

            foreach (var file in files)
            {
                if (file == null) continue;
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                var dest = Path.Combine(FileSystem.AppDataDirectory, $"memory_{Guid.NewGuid():N}{ext}");
                await using var src = await file.OpenReadAsync();
                await using var dst = File.Create(dest);
                await src.CopyToAsync(dst);

                newPaths.Add(dest);
                newMoments.Add(new MemoryMoment(Guid.NewGuid(), null, DateTime.Today, dest));
            }

            if (newPaths.Count == 0) return;

            var updated = SelectedMemory with
            {
                ImagePath = SelectedMemory.ImagePath ?? newPaths.FirstOrDefault(),
                ImagePaths = newPaths,
                Moments = newMoments
            };

            var idx = Memories.IndexOf(SelectedMemory);
            if (idx >= 0)
            {
                Memories[idx] = updated;
            }
            SelectedMemory = updated;
            SaveList("couple_memories", Memories);
            _ = SyncToServerAsync();
        }
        catch { }
    }

    [RelayCommand]
    private void DeleteMemory(MemoryItem? item)
    {
        if (item == null) return;
        if (SelectedMemory == item) SelectedMemory = null;
        Memories.Remove(item);
        SaveList("couple_memories", Memories);
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void OpenAddNoteSheet()
    {
        NewNoteTitle = string.Empty;
        NewNoteMessage = string.Empty;
        NewNoteUnlockDate = DateTime.Today.AddDays(7);
        IsAddNoteSheetVisible = true;
    }

    [RelayCommand]
    private void SaveLoveNote()
    {
        if (string.IsNullOrWhiteSpace(NewNoteTitle) || string.IsNullOrWhiteSpace(NewNoteMessage)) return;
        LoveNotes.Insert(0, new LoveNoteItem(
            Guid.NewGuid(),
            NewNoteTitle.Trim(),
            NewNoteMessage.Trim(),
            NewNoteUnlockDate,
            false));
        SaveList("couple_love_notes", LoveNotes);
        IsAddNoteSheetVisible = false;
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void DeleteLoveNote(LoveNoteItem? item)
    {
        if (item == null) return;
        LoveNotes.Remove(item);
        SaveList("couple_love_notes", LoveNotes);
        _ = SyncToServerAsync();
    }

    private async Task<string> UploadLocalImageIfNeededAsync(string path)
    {
        if (_stickerApi == null || string.IsNullOrWhiteSpace(path) || CoupleMediaHelper.IsRemoteUrl(path) || !File.Exists(path))
            return path;

        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            await using var stream = File.OpenRead(path);
            var uploadResult = await _stickerApi.UploadMediaAsync(stream, Path.GetFileName(path), contentType, MessageType.Image);
            if (uploadResult.IsSuccess && uploadResult.Value != null && !string.IsNullOrWhiteSpace(uploadResult.Value.FileUrl))
            {
                return uploadResult.Value.FileUrl;
            }
        }
        catch { }

        return path;
    }

    private async Task EnsureMemoriesUploadedAsync()
    {
        if (_stickerApi == null || Memories.Count == 0) return;

        var anyChanged = false;
        for (var i = 0; i < Memories.Count; i++)
        {
            var mem = Memories[i];
            var memChanged = false;

            var coverPath = mem.ImagePath;
            if (!string.IsNullOrWhiteSpace(coverPath))
            {
                var uploaded = await UploadLocalImageIfNeededAsync(coverPath);
                if (uploaded != coverPath)
                {
                    coverPath = uploaded;
                    memChanged = true;
                }
            }

            List<string>? newPaths = mem.ImagePaths;
            if (mem.ImagePaths != null && mem.ImagePaths.Count > 0)
            {
                var updatedList = new List<string>(mem.ImagePaths.Count);
                foreach (var p in mem.ImagePaths)
                {
                    var uploaded = await UploadLocalImageIfNeededAsync(p);
                    if (uploaded != p) memChanged = true;
                    updatedList.Add(uploaded);
                }
                newPaths = updatedList;
            }

            List<MemoryMoment>? newMoments = mem.Moments;
            if (mem.Moments != null && mem.Moments.Count > 0)
            {
                var updatedMoments = new List<MemoryMoment>(mem.Moments.Count);
                foreach (var m in mem.Moments)
                {
                    var uploaded = await UploadLocalImageIfNeededAsync(m.ImagePath);
                    if (uploaded != m.ImagePath)
                    {
                        memChanged = true;
                        updatedMoments.Add(m with { ImagePath = uploaded });
                    }
                    else
                    {
                        updatedMoments.Add(m);
                    }
                }
                newMoments = updatedMoments;
            }

            if (memChanged)
            {
                anyChanged = true;
                var updatedMem = mem with
                {
                    ImagePath = coverPath,
                    ImagePaths = newPaths,
                    Moments = newMoments
                };
                Memories[i] = updatedMem;
                if (SelectedMemory == mem)
                {
                    SelectedMemory = updatedMem;
                }
            }
        }

        if (anyChanged)
        {
            SaveList("couple_memories", Memories);
        }
    }

    private async Task SyncToServerAsync()
    {
        if (_coupleApi == null) return;
        try
        {
            await EnsureMemoriesUploadedAsync();

            var answers = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(TodayAnswer))
            {
                answers[$"{DateTime.Today:yyyyMMdd}"] = TodayAnswer.Trim();
            }

            await _coupleApi.SyncSpaceAsync(new SyncCoupleSpaceRequestClientDto(
                JsonSerializer.Serialize(SpecialDates),
                JsonSerializer.Serialize(Memories),
                JsonSerializer.Serialize(LoveNotes),
                JsonSerializer.Serialize(answers)));
        }
        catch { }
    }

    private static void SaveList<T>(string key, ObservableCollection<T> list)
    {
        try
        {
            Preferences.Default.Set(key, JsonSerializer.Serialize(list));
        }
        catch { }
    }
}
