using ChatApp.Mobile.Features.Authentication.Services;

namespace ChatApp.Mobile.Controls;

public partial class AvatarView : ContentView
{
    public static readonly BindableProperty AvatarUrlProperty =
        BindableProperty.Create(nameof(AvatarUrl), typeof(string), typeof(AvatarView), null,
            propertyChanged: (b, _, _) => ((AvatarView)b).OnAvatarUrlChanged());

    public static readonly BindableProperty FullNameProperty =
        BindableProperty.Create(nameof(FullName), typeof(string), typeof(AvatarView), string.Empty);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(AvatarView), 48.0,
            propertyChanged: (b, _, _) => ((AvatarView)b).OnSizeChanged());

    public static readonly BindableProperty IsOnlineProperty =
        BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(AvatarView), false);

    public string? AvatarUrl
    {
        get => (string?)GetValue(AvatarUrlProperty);
        set => SetValue(AvatarUrlProperty, value);
    }

    public string FullName
    {
        get => (string)GetValue(FullNameProperty);
        set => SetValue(FullNameProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    public double Radius => Size / 2.0;
    public double FontSize => Math.Max(10.0, Size * 0.38);
    public double BadgeSize => Math.Max(12.0, Size * 0.28);
    public bool HasAvatarUrl => !string.IsNullOrWhiteSpace(AvatarUrl);

    public string? ResolvedAvatarUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AvatarUrl)) return null;
            if (AvatarUrl.StartsWith("/")) return $"{ApiConfig.BaseUrl}{AvatarUrl}";
            return AvatarUrl;
        }
    }

    public AvatarView()
    {
        InitializeComponent();
    }

    private void OnAvatarUrlChanged()
    {
        OnPropertyChanged(nameof(HasAvatarUrl));
        OnPropertyChanged(nameof(ResolvedAvatarUrl));
    }

    private void OnSizeChanged()
    {
        OnPropertyChanged(nameof(Radius));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(BadgeSize));
    }
}
