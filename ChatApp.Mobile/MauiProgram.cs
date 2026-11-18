using CommunityToolkit.Maui;
using ChatApp.Mobile.Features.Common;
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

        // 1. Navigation & System Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // 2. ViewModels & Pages
        builder.Services.AddTransient<DesignSystemViewModel>();
        builder.Services.AddTransient<DesignSystemPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
