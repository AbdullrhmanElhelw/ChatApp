using System.Windows.Input;

namespace ChatApp.Mobile.Controls;

public partial class ChatEntry : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ChatEntry), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty LabelTextProperty =
        BindableProperty.Create(nameof(LabelText), typeof(string), typeof(ChatEntry), string.Empty,
            propertyChanged: (b, _, _) => ((ChatEntry)b).OnLabelChanged());

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ChatEntry), string.Empty);

    public static readonly BindableProperty IsPasswordProperty =
        BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(ChatEntry), false);

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

    public Color BorderStroke => HasError
        ? Color.FromArgb("#EF4444")
        : (App.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#334155") : Color.FromArgb("#CBD5E1"));

    public ICommand TogglePasswordCommand { get; }

    public ChatEntry()
    {
        TogglePasswordCommand = new Command(() =>
        {
            IsPassword = !IsPassword;
        });

        InitializeComponent();
    }

    private void OnLabelChanged() => OnPropertyChanged(nameof(HasLabel));

    private void OnErrorChanged()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(BorderStroke));
    }
}
