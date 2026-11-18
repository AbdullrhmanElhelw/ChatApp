using System.Globalization;

namespace ChatApp.Mobile.Converters;

public class AvatarColorConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.FromArgb("#0D9488"), // Teal
        Color.FromArgb("#0284C7"), // Sky Blue
        Color.FromArgb("#7C3AED"), // Violet
        Color.FromArgb("#DB2777"), // Pink
        Color.FromArgb("#EA580C"), // Orange
        Color.FromArgb("#16A34A"), // Green
        Color.FromArgb("#4F46E5"), // Indigo
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
