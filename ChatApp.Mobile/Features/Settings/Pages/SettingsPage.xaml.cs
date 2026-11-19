using ChatApp.Mobile.Features.Settings.ViewModels;

namespace ChatApp.Mobile.Features.Settings.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage() : this(IPlatformApplication.Current?.Services?.GetService<SettingsViewModel>() ?? new SettingsViewModel(null!, null!))
    {
    }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshState();
    }
}
