using Microsoft.Maui.Storage;
namespace Aquatir
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            AutoReturnSwitch.IsToggled = Preferences.Get("AutoReturnEnabled", false);
          //  ShowPriceSwitch.IsToggled = Preferences.Get("ShowPriceEnabled", false);
            ShowPackagedProductsSwitch.IsToggled = Preferences.Get("ShowPackagedProducts", true); // По умолчанию включено
            IgnoreColorsSwitch.IsToggled = Preferences.Get("IgnoreColors", false);

            // Подписка на события переключателей
            AutoReturnSwitch.Toggled += OnAutoReturnToggled;
            IgnoreColorsSwitch.Toggled += OnIgnoreColorsToggled;
          //  ShowPriceSwitch.Toggled += OnShowPriceToggled;
            ShowPackagedProductsSwitch.Toggled += OnShowPackagedProductsToggled;
        }

        private void OnAutoReturnToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("AutoReturnEnabled", e.Value);
        }
        private void OnIgnoreColorsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("IgnoreColors", e.Value);
        }


        /*  private void OnShowPriceToggled(object sender, ToggledEventArgs e)
          {
              Preferences.Set("ShowPriceEnabled", e.Value);
          } */

        private void OnShowPackagedProductsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("ShowPackagedProducts", e.Value);

            // Вызываем обновление продуктов
            if (Application.Current.MainPage is MainPage mainPage)
            {
                mainPage.ReloadProducts();
            }
        }
    }

}
