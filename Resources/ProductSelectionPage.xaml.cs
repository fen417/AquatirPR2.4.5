using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using System.Drawing;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
using System.Text.RegularExpressions;

namespace Aquatir
{
    public static class ProductCache
    {
        public static Dictionary<string, List<ProductItem>> CachedProducts { get; set; } = new Dictionary<string, List<ProductItem>>();
    }

    public partial class ProductSelectionPage : ContentPage
    {
        private readonly string _groupName;
        private readonly MainPage _mainPage;
        private const string LastLoadTimeKey = "LastLoadTime_";
        private static readonly HttpClient client = new HttpClient(); // Используем один экземпляр HttpClient

        public ProductSelectionPage(string groupName, MainPage mainPage)
        {
            InitializeComponent();
            _groupName = groupName;
            _mainPage = mainPage;
            LoadProducts();
        }

        public async void LoadProducts()
        {
            string loadTimeKey = LastLoadTimeKey + _groupName;
            bool isCacheAvailable = ProductCache.CachedProducts.ContainsKey(_groupName);
            DateTime lastLoadTime = DateTime.MinValue;

            if (Preferences.ContainsKey(loadTimeKey))
            {
                string lastLoadTimeString = Preferences.Get(loadTimeKey, string.Empty);
                if (!string.IsNullOrEmpty(lastLoadTimeString) && DateTime.TryParse(lastLoadTimeString, out DateTime parsedTime))
                {
                    lastLoadTime = parsedTime;
                }
            }

            if (!isCacheAvailable || (DateTime.Now - lastLoadTime).TotalHours >= 24)
            {
                Console.WriteLine("Загрузка всех групп продукции...");
                ShowLoadingIndicator();

                var loadedProducts = await LoadAllProductsFromUrl();
                foreach (var group in loadedProducts)
                {
                    ProductCache.CachedProducts[group.Key] = group.Value;
                }

                Preferences.Set(loadTimeKey, DateTime.Now.ToString("O"));
            }
            else
            {
                Console.WriteLine("Использование кэшированных данных.");
            }

            bool showPackagedProducts = Preferences.Get("ShowPackagedProducts", true);

            var filteredProducts = ProductCache.CachedProducts.ContainsKey(_groupName)
                ? ProductCache.CachedProducts[_groupName].Where(product =>
                {
                    if ((_groupName == "Холодное Копчение" || _groupName == "Вяленая Продукция") && !showPackagedProducts)
                    {
                        return !product.Name.EndsWith("УП.", StringComparison.OrdinalIgnoreCase);
                    }
                    return true;
                }).ToList()
                : new List<ProductItem>();

            ProductCollectionView.ItemsSource = filteredProducts;

            Console.WriteLine("Продукты загружены и отфильтрованы.");
            HideLoadingIndicator();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.Trim() ?? string.Empty;
            var filteredProducts = ProductCache.CachedProducts.ContainsKey(_groupName)
                ? ProductCache.CachedProducts[_groupName].Where(product =>
                    string.IsNullOrEmpty(searchText) ||
                    product.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<ProductItem>();

            ProductCollectionView.ItemsSource = filteredProducts;
        }

        private async Task<string> GetFileDownloadLinkFromYandex()
        {
            string yandexFilePath = "/productsCOLOR.json";  // Путь к файлу на Яндекс.Диске
            string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";  // OAuth токен

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");

                var response = await client.GetAsync($"https://cloud-api.yandex.net/v1/disk/resources/download?path={Uri.EscapeDataString(yandexFilePath)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<dynamic>(content);
                    return json.href;
                }
                else
                {
                    Console.WriteLine("Ошибка получения ссылки для скачивания: " + response.StatusCode);
                    return null;
                }
            }
        }
        private async Task<Dictionary<string, List<ProductItem>>> LoadAllProductsFromUrl()
        {
            string fileUrl = await GetFileDownloadLinkFromYandex();
            if (string.IsNullOrEmpty(fileUrl))
            {
                Console.WriteLine("Не удалось получить ссылку для скачивания.");
                return new Dictionary<string, List<ProductItem>>();
            }

            try
            {
                var response = await client.GetStringAsync(fileUrl);
                var productGroups = JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(response);
                return productGroups ?? new Dictionary<string, List<ProductItem>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                return new Dictionary<string, List<ProductItem>>();
            }
        }

        private void ShowLoadingIndicator()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
        }

        private void HideLoadingIndicator()
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }

        private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedProduct = e.CurrentSelection.FirstOrDefault() as ProductItem;
            if (selectedProduct != null)
            {
                // Удаление тегов <color> и </color> из имени продукта
                string cleanProductName = Regex.Replace(selectedProduct.Name, @"<\/?color.*?>", string.Empty);

                if (DeviceInfo.Platform == DevicePlatform.WinUI) // Для Windows (WinUI)
                {
                    var quantityPopup = new QuantityInputPopup();
                    quantityPopup.QuantityConfirmed += async (sender, quantity) =>
                    {
                        // Добавляем продукт в заказ
                        _mainPage.AddProductToOrder(cleanProductName, quantity);

                        if (Preferences.Get("AutoReturnEnabled", false))
                        {
                            await Navigation.PopAsync();
                        }
                    };
                    await Application.Current.MainPage.ShowPopupAsync(quantityPopup);
                }
                else if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    string result = await DisplayPromptAsync("Введите количество", $"Введите количество для {cleanProductName}", "OK", "Отмена", keyboard: Keyboard.Numeric);

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        result = result.Replace(",", ".");
                        if (decimal.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) && quantity > 0)
                        {
                            // Добавляем продукт в заказ
                            _mainPage.AddProductToOrder(cleanProductName, quantity);
                            await DisplayAlert("Успех", $"{cleanProductName} успешно добавлен(а) в заказ.", "OK");

                            if (Preferences.Get("AutoReturnEnabled", false))
                            {
                                await Navigation.PopAsync();
                            }
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Введите корректное количество.", "OK");
                        }
                    }
                }

                ProductCollectionView.SelectedItem = null;
            }
        }


        public class QuantityInputPopup : Popup
        {
            public Entry QuantityEntry { get; private set; }
            public Button ConfirmButton { get; private set; }

            public event EventHandler<decimal> QuantityConfirmed;

            public QuantityInputPopup()
            {
                QuantityEntry = new Entry
                {
                    Keyboard = Keyboard.Numeric,
                    Placeholder = "Введите количество",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.Start,
                    HeightRequest = 50,
                    FontSize = 18,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D30"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#E1E1E1")
                };

                QuantityEntry.TextChanged += (s, e) =>
                {
                    if (e.NewTextValue != null)
                    {
                        QuantityEntry.Text = e.NewTextValue.Replace(',', '.');
                    }
                };

                QuantityEntry.Completed += (s, e) => OnConfirmButtonClicked(s, e);

                ConfirmButton = new Button
                {
                    Text = "OK",
                    HorizontalOptions = LayoutOptions.Center,
                    HeightRequest = 50,
                    FontSize = 18,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#3C3F41"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#E1E1E1")
                };
                ConfirmButton.Clicked += OnConfirmButtonClicked;

                var stackLayout = new StackLayout
                {
                    Padding = new Thickness(20),
                    Spacing = 20,
                    Children = { QuantityEntry, ConfirmButton }
                };

                var contentLayout = new Frame
                {
                    Padding = new Thickness(10),
                    CornerRadius = 10,
                    Content = stackLayout,
                    HeightRequest = 200,
                    WidthRequest = 300,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D30")
                };

                Content = contentLayout;
                this.Opened += OnPopupOpened;
            }

            private async void OnPopupOpened(object sender, EventArgs e)
            {
                await Task.Delay(100);
                QuantityEntry.Focus();
            }

            private void OnConfirmButtonClicked(object sender, EventArgs e)
            {
                if (decimal.TryParse(QuantityEntry.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) && quantity > 0)
                {
                    QuantityConfirmed?.Invoke(this, quantity);
                    Close();
                }
                else
                {
                    Application.Current.MainPage.DisplayAlert("Ошибка", "Введите корректное количество.", "OK");
                }
            }
        }
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public void SelectProductsForOrder(Order order)
        {
            order.Products = ProductCache.CachedProducts.ContainsKey(_groupName)
                ? ProductCache.CachedProducts[_groupName]
                : new List<ProductItem>();
        }
    }
}
