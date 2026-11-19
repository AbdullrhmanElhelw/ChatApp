using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.CoupleSpace.Models;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile.Features.CoupleSpace.ViewModels;

public enum CoupleTab
{
    Dates,
    Memories,
    LoveNotes
}

public partial class CoupleSpaceViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

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
    [NotifyPropertyChangedFor(nameof(IsDatesTab))]
    [NotifyPropertyChangedFor(nameof(IsMemoriesTab))]
    [NotifyPropertyChangedFor(nameof(IsLoveNotesTab))]
    public partial CoupleTab ActiveTab { get; set; } = CoupleTab.Dates;

    public bool IsDatesTab => ActiveTab == CoupleTab.Dates;
    public bool IsMemoriesTab => ActiveTab == CoupleTab.Memories;
    public bool IsLoveNotesTab => ActiveTab == CoupleTab.LoveNotes;

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

    // Add Special Date Sheet
    [ObservableProperty]
    public partial bool IsAddDateSheetVisible { get; set; }

    [ObservableProperty]
    public partial string NewDateTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime NewDateValue { get; set; } = DateTime.Today;

    // Add Memory Sheet
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

    // Add Love Note Sheet
    [ObservableProperty]
    public partial bool IsAddNoteSheetVisible { get; set; }

    [ObservableProperty]
    public partial string NewNoteTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewNoteMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime NewNoteUnlockDate { get; set; } = DateTime.Today.AddDays(7);

    public CoupleSpaceViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "عالمنا الخاص";
        LoadAll();
    }

    public void LoadAll()
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
        catch
        {
            // Safe fallback
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
    }

    // Special Dates
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
    }

    [RelayCommand]
    private void DeleteSpecialDate(SpecialDateItem? item)
    {
        if (item == null) return;
        SpecialDates.Remove(item);
        SaveList("couple_special_dates", SpecialDates);
    }

    // Memories
    [RelayCommand]
    private void OpenAddMemorySheet()
    {
        NewMemoryTitle = string.Empty;
        NewMemoryNote = string.Empty;
        NewMemoryLocation = string.Empty;
        NewMemoryImagePath = null;
        IsAddMemorySheetVisible = true;
    }

    [RelayCommand]
    private async Task PickMemoryPhotoAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "اختر صورة للذكرى",
                FileTypes = FilePickerFileType.Images
            });
            if (file == null) return;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var dest = Path.Combine(FileSystem.AppDataDirectory, $"memory_{Guid.NewGuid():N}{ext}");
            await using var src = await file.OpenReadAsync();
            await using var dst = File.Create(dest);
            await src.CopyToAsync(dst);
            NewMemoryImagePath = dest;
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
            NewMemoryImagePath));
        SaveList("couple_memories", Memories);
        IsAddMemorySheetVisible = false;
    }

    [RelayCommand]
    private void DeleteMemory(MemoryItem? item)
    {
        if (item == null) return;
        Memories.Remove(item);
        SaveList("couple_memories", Memories);
    }

    // Love Notes
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
    }

    [RelayCommand]
    private void DeleteLoveNote(LoveNoteItem? item)
    {
        if (item == null) return;
        LoveNotes.Remove(item);
        SaveList("couple_love_notes", LoveNotes);
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
