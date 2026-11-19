using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Common;
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
    public partial string? NewMemoryImagePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMemoryPhotosLabel))]
    public partial List<string> NewMemoryImagePaths { get; set; } = new();

    public string SelectedMemoryPhotosLabel => NewMemoryImagePaths.Count > 0
        ? $"تم اختيار {NewMemoryImagePaths.Count} صور للذكرى"
        : "اختيار صورة أو أكثر للذكرى";

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
        IConversationApiService? conversationApi = null)
    {
        _navigationService = navigationService;
        _coupleApi = coupleApi;
        _conversationApi = conversationApi;
        Title = "عالمنا الخاص";
        LoadLocal();
    }

    public async Task LoadFromServerAsync()
    {
        if (_coupleApi == null) return;
        try
        {
            var res = await _coupleApi.GetMySpaceAsync();
            if (res.IsSuccess && res.Value != null)
            {
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
        Memories.Insert(0, new MemoryItem(
            Guid.NewGuid(),
            NewMemoryTitle.Trim(),
            NewMemoryNote.Trim(),
            string.IsNullOrWhiteSpace(NewMemoryLocation) ? null : NewMemoryLocation.Trim(),
            DateTime.Today,
            NewMemoryImagePath,
            NewMemoryImagePaths.ToList()));
        SaveList("couple_memories", Memories);
        IsAddMemorySheetVisible = false;
        _ = SyncToServerAsync();
    }

    [RelayCommand]
    private void DeleteMemory(MemoryItem? item)
    {
        if (item == null) return;
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

    private async Task SyncToServerAsync()
    {
        if (_coupleApi == null) return;
        try
        {
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
