using System.Runtime.Versioning;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;
using AndroidX.Core.App;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification;

namespace Aquatir;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    [ObsoletedOSPlatform("Android34.0")]
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Запрос разрешений на уведомления (для Android 13 и выше)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.PostNotifications }, 0);
        }

        // Создаем список каналов уведомлений
        var channelRequests = new List<NotificationChannelRequest>
        {
            new NotificationChannelRequest
            {
                Id = "default_channel", // Уникальный ID канала
                Name = "Default Channel", // Название канала
                Description = "Default notifications channel", // Описание канала
                Importance = (AndroidImportance)NotificationImportance.Default // Важность уведомлений
            }
        };

        // Инициализация каналов уведомлений
        Plugin.LocalNotification.LocalNotificationCenter.CreateNotificationChannels(channelRequests);

        // Устанавливаем цвет строки состояния
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#2C3E50"));
    }
}
