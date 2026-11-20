using ChatApp.Mobile.Features.Chat.Pages;
using ChatApp.Mobile.Features.Conversations.Pages;
using ChatApp.Mobile.Features.CoupleSpace.Pages;
using ChatApp.Mobile.Features.Settings.Pages;
using ChatApp.Mobile.Features.Stickers.Pages;
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
        Routing.RegisterRoute(AppRoutes.Chat, typeof(ChatPage));
        Routing.RegisterRoute(AppRoutes.StickerStudio, typeof(StickerStudioPage));
        Routing.RegisterRoute($"chat/{AppRoutes.StickerStudio}", typeof(StickerStudioPage));
        Routing.RegisterRoute(AppRoutes.Settings, typeof(SettingsPage));
        Routing.RegisterRoute(AppRoutes.CoupleSpace, typeof(CoupleSpacePage));
    }
}
