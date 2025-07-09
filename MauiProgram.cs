using CommunityToolkit.Maui;
using Microsoft.Maui; // Возможно, это не нужно, если только вы явно не используете что-то из Microsoft.Maui
using Plugin.LocalNotification;
using Microsoft.Maui.LifecycleEvents;
// using CommunityToolkit.Maui; // Дублирование, можно удалить
using CommunityToolkit.Maui.Media; // Если вы используете MediaElement, оставьте это

namespace Aquatir
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>()
                   .UseMauiCommunityToolkit()
                   .UseLocalNotification()
                   .ConfigureFonts(fonts =>
                   {
                       fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                       fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                   })
                   .UseMauiCommunityToolkitMediaElement();
#if ANDROID
            // Для Android регистрируем конкретную реализацию
            builder.Services.AddSingleton<ISpeechToTextService, SpeechToTextService>();
#else
            // Для других платформ регистрируем заглушку, чтобы приложение могло запуститься
            builder.Services.AddSingleton<ISpeechToTextService, NoOpSpeechToTextService>();
#endif
            // ***********************************************

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