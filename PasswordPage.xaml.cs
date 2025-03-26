using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Aquatir
{
    public partial class PasswordPage : ContentPage
    {
        private const string UserAccessCode = "201124";
        private const string ManagerAccessCode = "160400";
        private static readonly HttpClient client = new HttpClient();

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
                await DisplayAlert("Îøèáêà", "Ïîæàëóéñòà, çàïîëíèòå âñå ïîëÿ.", "OK");
                return;
            }

            if (enteredCode == UserAccessCode)
            {
                Preferences.Set("AuthorizationType", "User");
                Preferences.Set("UserCity", selectedCity);
                Preferences.Set("UserShopName", shopName);
                Preferences.Set("IsAuthorized", true);
                NavigateToMainPage();
                StartBackgroundProductLoading(); // Çàïóñê ôîíîâîé çàãðóçêè
            }
            else
            {
                await DisplayAlert("Îøèáêà", "Íåâåðíûé êîä äîñòóïà.", "OK");
            }
        }

        private async void OnManagerLoginClicked(object sender, EventArgs e)
        {
            string enteredManagerCode = ManagerCodeEntry.Text;

            if (string.IsNullOrWhiteSpace(enteredManagerCode))
            {
                await DisplayAlert("Îøèáêà", "Ââåäèòå êîä ìåíåäæåðà.", "OK");
                return;
            }

            if (enteredManagerCode == ManagerAccessCode)
            {
                Preferences.Set("AuthorizationType", "Manager");
                Preferences.Set("IsAuthorized", true);
                NavigateToMainPage();
                StartBackgroundProductLoading(); // Çàïóñê ôîíîâîé çàãðóçêè
            }
            else
            {
                await DisplayAlert("Îøèáêà", "Íåâåðíûé êîä äîñòóïà ìåíåäæåðà.", "OK");
            }
        }

private void NavigateToMainPage()
{
    Console.WriteLine("[PasswordPage] Переход на главную страницу...");
    try
    {
        // Универсальный подход для всех платформ
        Application.Current.MainPage = new AppShell();
        
        Console.WriteLine("[PasswordPage] Переход на главную страницу выполнен.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PasswordPage] Ошибка при переходе на главную страницу: {ex.Message}");
    }
}
        private void StartBackgroundProductLoading()
        {
            // Çàïóñê ôîíîâîé çàäà÷è äëÿ çàãðóçêè äàííûõ
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("Çàãðóçêà äàííûõ î ïðîäóêöèè â ôîíîâîì ðåæèìå...");
                    await LoadAllProductsFromUrl();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Îøèáêà ïðè çàãðóçêå äàííûõ: {ex.Message}");
                }
            });
        }

        private async Task LoadAllProductsFromUrl()
        {
            string fileUrl = await GetFileDownloadLinkFromYandex();
            if (string.IsNullOrEmpty(fileUrl))
            {
                Console.WriteLine("Íå óäàëîñü ïîëó÷èòü ññûëêó äëÿ ñêà÷èâàíèÿ.");
                return;
            }

            try
            {
                var response = await client.GetAsync(fileUrl);
                if (response.IsSuccessStatusCode)
                {
                    var lastModifiedHeader = response.Content.Headers.LastModified;
                    if (lastModifiedHeader.HasValue)
                    {
                        ProductCache.SaveLastModified(lastModifiedHeader.Value.DateTime);
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Äàííûå óñïåøíî çàãðóæåíû ñ ñåðâåðà.");
                    var productGroups = JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(content);
                    if (productGroups != null)
                    {
                        var databaseService = new DatabaseService();
                        await databaseService.SaveProductGroupsAsync(productGroups);

                        Console.WriteLine("Äàííûå óñïåøíî ñîõðàíåíû â áàçó äàííûõ.");
                    }
                }
                else
                {
                    Console.WriteLine("Îøèáêà çàãðóçêè: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Îøèáêà çàãðóçêè: {ex.Message}");
            }
        }

        private async Task<string> GetFileDownloadLinkFromYandex()
        {
            string yandexFilePath = "/productsCOLOR.json";  // Ïóòü ê ôàéëó íà ßíäåêñ.Äèñêå
            string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";  // OAuth òîêåí

            client.DefaultRequestHeaders.Clear();
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
                Console.WriteLine("Îøèáêà ïîëó÷åíèÿ ññûëêè äëÿ ñêà÷èâàíèÿ: " + response.StatusCode);
                return null;
            }
        }
    }
}
