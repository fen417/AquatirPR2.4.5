using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;

namespace Aquatir
{
    public partial class PasswordPage : ContentPage
    {
        private const string UserAccessCode = "201124";
        private const string ManagerAccessCode = "160400";

        public PasswordPage()
        {
            InitializeComponent();
        }

        private async void OnUserLoginClicked(object sender, EventArgs e)
        {
            string selectedCity = CityPicker.SelectedItem?.ToString();
            string shopName = ShopNameEntry.Text;
            string enteredCode = AccessCodeEntry.Text;

            if (string.IsNullOrWhiteSpace(selectedCity) || string.IsNullOrWhiteSpace(shopName) || string.IsNullOrWhiteSpace(enteredCode))
            {
                await DisplayAlert("Ошибка", "Пожалуйста, заполните все поля.", "OK");
                return;
            }

            if (enteredCode == UserAccessCode)
            {
                Preferences.Set("AuthorizationType", "User");
                Preferences.Set("UserCity", selectedCity);
                Preferences.Set("UserShopName", shopName);
                Preferences.Set("IsAuthorized", true);
                NavigateToMainPage();
            }
            else
            {
                await DisplayAlert("Ошибка", "Неверный код доступа.", "OK");
            }
        }

        private async void OnManagerLoginClicked(object sender, EventArgs e)
        {
            string enteredManagerCode = ManagerCodeEntry.Text;

            if (string.IsNullOrWhiteSpace(enteredManagerCode))
            {
                await DisplayAlert("Ошибка", "Введите код менеджера.", "OK");
                return;
            }

            if (enteredManagerCode == ManagerAccessCode)
            {
                Preferences.Set("AuthorizationType", "Manager");
                Preferences.Set("IsAuthorized", true);
                NavigateToMainPage();
            }
            else
            {
                await DisplayAlert("Ошибка", "Неверный код доступа менеджера.", "OK");
            }
        }

        private void NavigateToMainPage()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                Application.Current.MainPage = new AppShell();
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                Application.Current.MainPage = new MainPage();
            }
            else
            {
                Application.Current.MainPage = new MainPage();
            }
        }
    }
}
