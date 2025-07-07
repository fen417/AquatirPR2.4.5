using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;

namespace Aquatir
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Регистрация маршрутов для всех платформ
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("PasswordPage", typeof(PasswordPage));
            Routing.RegisterRoute(nameof(ProductSelectionPage), typeof(ProductSelectionPage));
            Routing.RegisterRoute(nameof(OrderDetailsPage), typeof(OrderDetailsPage));
            // Добавление содержимого для всех платформ
            if (Items.Count == 0)
            {
                Items.Add(new ShellContent
                {
                    ContentTemplate = new DataTemplate(typeof(PasswordPage)),
                    Route = "PasswordPage",
                    Title = "Авторизация"
                });
                Items.Add(new ShellContent
                {
                    ContentTemplate = new DataTemplate(typeof(MainPage)),
                    Route = "MainPage",
                    Title = "Главная"
                });
            }
        }
    }
}