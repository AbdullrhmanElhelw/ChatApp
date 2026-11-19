using CommunityToolkit.Maui;
using ChatApp.Mobile.Features.Authentication.Pages;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Authentication.ViewModels;
using ChatApp.Mobile.Features.Chat.Pages;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Chat.ViewModels;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.Pages;
using ChatApp.Mobile.Features.Conversations.Services;
using ChatApp.Mobile.Features.Conversations.ViewModels;
using ChatApp.Mobile.Features.Stickers.Pages;
using ChatApp.Mobile.Features.Stickers.ViewModels;
using ChatApp.Mobile.Navigation;
using Microsoft.Extensions.Logging;

namespace ChatApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("OpenSans-Regular.ttf", "CairoRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "CairoBold");
            });

#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BetweenUsBorderlessEntry", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("BetweenUsBorderlessEditor", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });
#endif

        // 1. Navigation & Core Infrastructure Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();

        // 2. HTTP & API Services
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        });
        builder.Services.AddSingleton<IAuthApiService, AuthApiService>();
        builder.Services.AddSingleton<IConversationApiService, ConversationApiService>();
        builder.Services.AddSingleton<IChatHubService, ChatHubService>();
        builder.Services.AddSingleton<IMessageApiService, MessageApiService>();
        builder.Services.AddSingleton<IStickerApiService, StickerApiService>();

        // 3. ViewModels
        builder.Services.AddTransient<SplashViewModel>();
        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ProfileSetupViewModel>();
        builder.Services.AddTransient<ConversationsViewModel>();
        builder.Services.AddTransient<UserSearchViewModel>();
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<StickerStudioViewModel>();
        builder.Services.AddTransient<DesignSystemViewModel>();

        // 4. Pages
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ProfileSetupPage>();
        builder.Services.AddTransient<ConversationsPage>();
        builder.Services.AddTransient<UserSearchPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<StickerStudioPage>();
        builder.Services.AddTransient<DesignSystemPage>();

        // 5. Native Platform UI Tweaks: Strip default ugly Windows borders on Entry & Editor
#if WINDOWS
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BorderlessEntry", (handler, view) =>
        {
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        });
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("BorderlessEditor", (handler, view) =>
        {
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
