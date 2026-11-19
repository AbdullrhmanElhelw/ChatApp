using ChatApp.Mobile.Features.CoupleSpace.ViewModels;

namespace ChatApp.Mobile.Features.CoupleSpace.Pages;

public partial class CoupleSpacePage : ContentPage
{
    private readonly CoupleSpaceViewModel _viewModel;

    public CoupleSpacePage() : this(IPlatformApplication.Current?.Services?.GetService<CoupleSpaceViewModel>() ?? new CoupleSpaceViewModel(null!))
    {
    }

    public CoupleSpacePage(CoupleSpaceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadFromServerAsync();
    }
}
