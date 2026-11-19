using System.Windows.Input;

namespace ChatApp.Mobile.Controls;

public partial class ChatEntry : ContentView
{
    private bool _isFocused;
    private bool _hasPasswordMode;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ChatEntry), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty LabelTextProperty =
        BindableProperty.Create(nameof(LabelText), typeof(string), typeof(ChatEntry), string.Empty,
            propertyChanged: (b, _, _) => ((ChatEntry)b).OnLabelChanged());

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ChatEntry), string.Empty);

    public static readonly BindableProperty IsPasswordProperty =
        BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(ChatEntry), false,
            propertyChanged: (b, _, newVal) => ((ChatEntry)b).OnPasswordChanged((bool)newVal));

    public static readonly BindableProperty ErrorMessageProperty =
        BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(ChatEntry), string.Empty,
            propertyChanged: (b, _, _) => ((ChatEntry)b).OnErrorChanged());

    public static readonly BindableProperty KeyboardProperty =
        BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(ChatEntry), Keyboard.Default);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public bool HasLabel => !string.IsNullOrWhiteSpace(LabelText);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool ShowPasswordToggle => _hasPasswordMode;
    public string PasswordToggleGlyph => IsPassword ? "👁️" : "🙈";

    public Color BorderStroke
    {
        get
        {
            if (HasError) return Color.FromArgb("#EF4444");
            if (_isFocused) return Color.FromArgb("#9B4DDB"); // Primary Violet focus border
            return Application.Current?.RequestedTheme != AppTheme.Light
                ? Color.FromArgb("#39274F")
                : Color.FromArgb("#E9D7F5");
        }
    }

    public ICommand TogglePasswordCommand { get; }

    public ChatEntry()
    {
        TogglePasswordCommand = new Command(() =>
        {
            IsPassword = !IsPassword;
        });

        InitializeComponent();

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (_, _) => OnPropertyChanged(nameof(BorderStroke));
        }
    }

    private void OnEntryFocused(object? sender, FocusEventArgs e)
    {
        _isFocused = true;
        OnPropertyChanged(nameof(BorderStroke));
    }

    private void OnEntryUnfocused(object? sender, FocusEventArgs e)
    {
        _isFocused = false;
        OnPropertyChanged(nameof(BorderStroke));
    }

    private void OnPasswordChanged(bool isPassword)
    {
        if (isPassword) _hasPasswordMode = true;
        OnPropertyChanged(nameof(ShowPasswordToggle));
        OnPropertyChanged(nameof(PasswordToggleGlyph));
    }

    private void OnLabelChanged() => OnPropertyChanged(nameof(HasLabel));

    private void OnErrorChanged()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(BorderStroke));
    }
}
