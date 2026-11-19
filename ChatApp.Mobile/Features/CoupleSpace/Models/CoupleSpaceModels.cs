namespace ChatApp.Mobile.Features.CoupleSpace.Models;

public record SpecialDateItem(
    Guid Id,
    string Title,
    DateTime Date,
    bool IsAnnualCountdown = true
)
{
    public string FormattedDate => Date.ToString("dd MMMM yyyy");

    public string CountdownBadge
    {
        get
        {
            var today = DateTime.Today;
            if (!IsAnnualCountdown)
            {
                var diff = (Date.Date - today).Days;
                return diff switch
                {
                    0 => "اليوم!",
                    > 0 => $"باقي {diff} يوم",
                    _ => $"منذ {Math.Abs(diff)} يوم"
                };
            }

            var next = new DateTime(today.Year, Date.Month, Date.Day);
            if (next < today) next = next.AddYears(1);
            var daysLeft = (next - today).Days;
            return daysLeft == 0 ? "اليوم!" : $"باقي {daysLeft} يوم";
        }
    }
}

public record MemoryItem(
    Guid Id,
    string Title,
    string Note,
    string? Location,
    DateTime MemoryDate,
    string? ImagePath
)
{
    public string FormattedDate => MemoryDate.ToString("dd MMM yyyy");
    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath);
    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);
}

public record LoveNoteItem(
    Guid Id,
    string Title,
    string Message,
    DateTime UnlockDate,
    bool IsOpened
)
{
    public bool IsUnlocked => DateTime.Today >= UnlockDate.Date;
    public bool IsLocked => !IsUnlocked;
    public string StatusLabel => IsUnlocked ? "جاهزة للفتح الآن" : $"تُفتح في {UnlockDate:dd MMM yyyy}";
    public string DisplayPreview => IsUnlocked ? Message : "رسالة حب مغلقة حتى يحين موعدها...";
}
