using CommunityToolkit.Maui;
using Plugin.LocalNotification;
using Microsoft.Maui.LifecycleEvents;

namespace Aquatir
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows =>
            {
                windows.OnWindowCreated((window) =>
                {
                    window.Title = "Aquatir Заявки";
                });
            });
        });
#endif

            return builder.Build();
        }
    }
}
