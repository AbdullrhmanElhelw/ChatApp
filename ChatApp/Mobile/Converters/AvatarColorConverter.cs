using System.Globalization;

namespace ChatApp.Mobile.Converters;

public class AvatarColorConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.FromArgb("#7025B8"), // Primary Purple
        Color.FromArgb("#9B4DDB"), // Violet
        Color.FromArgb("#F044A5"), // Pink
        Color.FromArgb("#6D28D9"), // Royal Violet
        Color.FromArgb("#C026D3"), // Fuchsia
        Color.FromArgb("#4E1688"), // Deep Violet
        Color.FromArgb("#DB2777"), // Rose Pink
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
        {
            return Palette[0];
        }

        var hash = Math.Abs(name.GetHashCode());
        return Palette[hash % Palette.Length];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
