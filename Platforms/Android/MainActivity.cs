using System.Runtime.Versioning;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace Aquatir;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    [ObsoletedOSPlatform("Android34.0")]
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Устанавливаем цвет строки состояния
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#2C3E50"));
    }
}
