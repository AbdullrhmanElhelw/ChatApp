using ChatApp.Mobile.Features.CoupleSpace.ViewModels;

namespace ChatApp.Mobile.Features.CoupleSpace.Pages;

public partial class CoupleSpacePage : ContentPage
{
    public CoupleSpacePage() : this(IPlatformApplication.Current?.Services?.GetService<CoupleSpaceViewModel>() ?? new CoupleSpaceViewModel(null!))
    {
    }

    public CoupleSpacePage(CoupleSpaceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
