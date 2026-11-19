namespace ChatApp.Mobile.Features.Common;

public partial class DesignSystemPage : ContentPage
{
    public DesignSystemPage() : this(IPlatformApplication.Current?.Services?.GetService<DesignSystemViewModel>() ?? new DesignSystemViewModel())
    {
    }

    public DesignSystemPage(DesignSystemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
