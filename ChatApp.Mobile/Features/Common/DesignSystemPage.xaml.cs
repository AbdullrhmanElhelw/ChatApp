namespace ChatApp.Mobile.Features.Common;

public partial class DesignSystemPage : ContentPage
{
    public DesignSystemPage(DesignSystemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
