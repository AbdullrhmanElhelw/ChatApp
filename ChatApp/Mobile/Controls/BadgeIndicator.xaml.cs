namespace ChatApp.Mobile.Controls;

public partial class BadgeIndicator : ContentView
{
    public static readonly BindableProperty CountProperty =
        BindableProperty.Create(nameof(Count), typeof(int), typeof(BadgeIndicator), 0,
            propertyChanged: (b, _, _) => ((BadgeIndicator)b).OnCountChanged());

    public static readonly BindableProperty BadgeColorProperty =
        BindableProperty.Create(nameof(BadgeColor), typeof(Color), typeof(BadgeIndicator), Color.FromArgb("#0D9488"));

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public Color BadgeColor
    {
        get => (Color)GetValue(BadgeColorProperty);
        set => SetValue(BadgeColorProperty, value);
    }

    public bool HasCount => Count > 0;
    public string FormattedCount => Count > 99 ? "99+" : Count.ToString();

    public BadgeIndicator()
    {
        InitializeComponent();
    }

    private void OnCountChanged()
    {
        OnPropertyChanged(nameof(HasCount));
        OnPropertyChanged(nameof(FormattedCount));
    }
}
