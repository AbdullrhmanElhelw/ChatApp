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

    public Color ButtonBackground => ButtonType switch
    {
        ChatButtonType.Primary => Color.FromArgb("#0D9488"),
        ChatButtonType.Secondary => Color.FromArgb("#F1F5F9"),
        ChatButtonType.Danger => Color.FromArgb("#EF4444"),
        _ => Colors.Transparent
    };

    public Color TextColor => ButtonType switch
    {
        ChatButtonType.Secondary => Color.FromArgb("#0F172A"),
        ChatButtonType.Ghost => Color.FromArgb("#0D9488"),
        _ => Colors.White
    };

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
    }

    private void OnButtonTypeChanged()
    {
        OnPropertyChanged(nameof(ButtonBackground));
        OnPropertyChanged(nameof(TextColor));
    }
}
