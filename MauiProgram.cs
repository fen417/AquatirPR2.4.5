using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

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
