using ChatApp.Mobile.Features.Authentication.Services;

namespace ChatApp.Mobile.Features.CoupleSpace.Models;

internal static class CoupleMediaHelper
{
    public static bool IsRemoteUrl(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("/"));

    public static bool IsAvailableImage(string? path) =>
        !string.IsNullOrWhiteSpace(path) && (IsRemoteUrl(path) || File.Exists(path));

    public static string? ResolveImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("/")) return $"{ApiConfig.BaseUrl}{path}";
        return path;
    }
}

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

public record MemoryMoment(
    Guid Id,
    string? Caption,
    DateTime MomentDate,
    string ImagePath,
    string? Title = null,
    TimeSpan? MomentTime = null
)
{
    public string FormattedDate => MomentTime.HasValue
        ? $"{MomentDate:MMM dd, yyyy} • {DateTime.Today.Add(MomentTime.Value):hh:mm tt}"
        : MomentDate.ToString("MMM dd, yyyy");
    public string? DisplayImageUrl => CoupleMediaHelper.ResolveImageUrl(ImagePath);
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasCaption => !string.IsNullOrWhiteSpace(Caption);
    public bool HasImage => CoupleMediaHelper.IsAvailableImage(ImagePath);
}

public record MemoryItem(
    Guid Id,
    string Title,
    string Note,
    string? Location,
    DateTime MemoryDate,
    string? ImagePath = null,
    List<string>? ImagePaths = null,
    List<MemoryMoment>? Moments = null
)
{
    public string FormattedDate => MemoryDate.ToString("MMM dd, yyyy");

    public List<string> DisplayImages
    {
        get
        {
            var list = new List<string>();
            if (Moments != null)
            {
                list.AddRange(Moments
                    .Select(m => m.ImagePath)
                    .Where(CoupleMediaHelper.IsAvailableImage)
                    .Select(p => CoupleMediaHelper.ResolveImageUrl(p)!));
            }
            if (list.Count == 0 && ImagePaths != null)
            {
                list.AddRange(ImagePaths
                    .Where(CoupleMediaHelper.IsAvailableImage)
                    .Select(p => CoupleMediaHelper.ResolveImageUrl(p)!));
            }
            if (list.Count == 0 && CoupleMediaHelper.IsAvailableImage(ImagePath))
            {
                list.Add(CoupleMediaHelper.ResolveImageUrl(ImagePath)!);
            }
            return list;
        }
    }

    public List<MemoryMoment> DisplayMoments
    {
        get
        {
            if (Moments != null && Moments.Count > 0)
            {
                var valid = Moments.Where(m => CoupleMediaHelper.IsAvailableImage(m.ImagePath) || m.HasTitle || m.HasCaption).ToList();
                if (valid.Count > 0) return valid;
            }

            return DisplayImages
                .Select((path, index) => new MemoryMoment(
                    Guid.NewGuid(),
                    index == 0 && !string.IsNullOrWhiteSpace(Note) ? Note : null,
                    MemoryDate,
                    path))
                .ToList();
        }
    }

    public string? CoverImage => DisplayImages.FirstOrDefault();
    public bool HasImages => DisplayImages.Count > 0;
    public bool HasMultipleImages => DisplayImages.Count > 1;
    public string ImageCountBadge => DisplayImages.Count == 1 ? "صورة واحدة" : $"{DisplayImages.Count} صور";
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
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
