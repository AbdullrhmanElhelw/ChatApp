using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Navigation;

namespace ChatApp.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute(AppRoutes.DesignSystem, typeof(DesignSystemPage));
    }
}
