using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
namespace Aquatir;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        if (DeviceInfo.Platform != DevicePlatform.Android)
        {
            this.Items.Clear(); // Убираем TabBar для других платформ
            this.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(MainPage)),
                Route = "MainPage",
                Title = "Главная"
            });

            this.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(PasswordPage)),
                Route = "PasswordPage",
                Title = "Авторизация"
            });
        }
    }
}
