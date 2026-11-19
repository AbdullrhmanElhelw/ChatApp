using ChatApp.Mobile.Features.Conversations.Pages;
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
        Routing.RegisterRoute(AppRoutes.UserSearch, typeof(UserSearchPage));
    }
}
