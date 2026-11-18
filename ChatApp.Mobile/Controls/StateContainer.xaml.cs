using System.Windows.Input;

namespace ChatApp.Mobile.Controls;

public enum ViewState
{
    Content,
    Loading,
    Empty,
    Error
}

[ContentProperty(nameof(NormalContent))]
public partial class StateContainer : ContentView
{
    public static readonly BindableProperty CurrentStateProperty =
        BindableProperty.Create(nameof(CurrentState), typeof(ViewState), typeof(StateContainer), ViewState.Content,
            propertyChanged: (b, _, _) => ((StateContainer)b).OnStateChanged());

    public static readonly BindableProperty NormalContentProperty =
        BindableProperty.Create(nameof(NormalContent), typeof(View), typeof(StateContainer), null);

    public static readonly BindableProperty LoadingTextProperty =
        BindableProperty.Create(nameof(LoadingText), typeof(string), typeof(StateContainer), "جاري التحميل...");

    public static readonly BindableProperty EmptyTitleProperty =
        BindableProperty.Create(nameof(EmptyTitle), typeof(string), typeof(StateContainer), "لا توجد عناصر");

    public static readonly BindableProperty EmptyMessageProperty =
        BindableProperty.Create(nameof(EmptyMessage), typeof(string), typeof(StateContainer), "لم يتم العثور على أي بيانات لعرضها هنا.");

    public static readonly BindableProperty ErrorMessageProperty =
        BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(StateContainer), "حدث خطأ ما أثناء تحميل البيانات.");

    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(StateContainer), null,
            propertyChanged: (b, _, _) => ((StateContainer)b).OnRetryCommandChanged());

    public ViewState CurrentState
    {
        get => (ViewState)GetValue(CurrentStateProperty);
        set => SetValue(CurrentStateProperty, value);
    }

    public View? NormalContent
    {
        get => (View?)GetValue(NormalContentProperty);
        set => SetValue(NormalContentProperty, value);
    }

    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public string EmptyTitle
    {
        get => (string)GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public bool IsContentVisible => CurrentState == ViewState.Content;
    public bool IsLoadingVisible => CurrentState == ViewState.Loading;
    public bool IsEmptyVisible => CurrentState == ViewState.Empty;
    public bool IsErrorVisible => CurrentState == ViewState.Error;
    public bool HasRetryCommand => RetryCommand != null;

    public StateContainer()
    {
        InitializeComponent();
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(IsContentVisible));
        OnPropertyChanged(nameof(IsLoadingVisible));
        OnPropertyChanged(nameof(IsEmptyVisible));
        OnPropertyChanged(nameof(IsErrorVisible));
    }

    private void OnRetryCommandChanged()
    {
        OnPropertyChanged(nameof(HasRetryCommand));
    }
}
