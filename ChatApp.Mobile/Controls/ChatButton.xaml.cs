using System.Windows.Input;

namespace ChatApp.Mobile.Controls;

public enum ChatButtonType
{
    Primary,
    Secondary,
    Ghost,
    Danger
}

public partial class ChatButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ChatButton), string.Empty);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ChatButton), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ChatButton), null);

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(ChatButton), false);

    public static readonly BindableProperty ButtonTypeProperty =
        BindableProperty.Create(nameof(ButtonType), typeof(ChatButtonType), typeof(ChatButton), ChatButtonType.Primary,
            propertyChanged: (b, _, _) => ((ChatButton)b).OnButtonTypeChanged());

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public ChatButtonType ButtonType
    {
        get => (ChatButtonType)GetValue(ButtonTypeProperty);
        set => SetValue(ButtonTypeProperty, value);
    }

    public ICommand TapCommand { get; }

    public Brush ButtonBrush
    {
        get
        {
            var isDark = Application.Current?.RequestedTheme != AppTheme.Light;
            return ButtonType switch
            {
                ChatButtonType.Primary => new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#7025B8"), 0.0f),
                        new GradientStop(Color.FromArgb("#F044A5"), 1.0f)
                    },
                    new Point(0, 0),
                    new Point(1, 1)),
                ChatButtonType.Secondary => new SolidColorBrush(isDark ? Color.FromArgb("#281A3E") : Color.FromArgb("#F7ECFA")),
                ChatButtonType.Danger => new SolidColorBrush(Color.FromArgb("#EF4444")),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
    }

    public Color BorderColor
    {
        get
        {
            var isDark = Application.Current?.RequestedTheme != AppTheme.Light;
            return ButtonType == ChatButtonType.Secondary
                ? (isDark ? Color.FromArgb("#39274F") : Color.FromArgb("#E9D7F5"))
                : Colors.Transparent;
        }
    }

    public double BorderWidth => ButtonType == ChatButtonType.Secondary ? 1 : 0;

    public Color TextColor
    {
        get
        {
            var isDark = Application.Current?.RequestedTheme != AppTheme.Light;
            return ButtonType switch
            {
                ChatButtonType.Secondary => isDark ? Color.FromArgb("#FFF5FF") : Color.FromArgb("#180B35"),
                ChatButtonType.Ghost => Color.FromArgb("#F044A5"),
                _ => Color.FromArgb("#FFF5FF")
            };
        }
    }

    public ChatButton()
    {
        TapCommand = new Command(() =>
        {
            if (!IsLoading && Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        });

        InitializeComponent();

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (_, _) => OnButtonTypeChanged();
        }
    }

    private void OnButtonTypeChanged()
    {
        OnPropertyChanged(nameof(ButtonBrush));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(BorderWidth));
        OnPropertyChanged(nameof(TextColor));
    }
}
