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
                await DisplayAlert("������", "����������, ��������� ��� ����.", "OK");
                return;
            }

            if (enteredCode == UserAccessCode)
            {
                Preferences.Set("AuthorizationType", "User");
                Preferences.Set("UserCity", selectedCity);
                Preferences.Set("UserShopName", shopName);
                Preferences.Set("IsAuthorized", true);
                NavigateToMainPage();
                StartBackgroundProductLoading(); // ������ ������� ��������
            }
            else
            {
                await DisplayAlert("������", "�������� ��� �������.", "OK");
            }
        }

        private async void OnManagerLoginClicked(object sender, EventArgs e)
        {
            string enteredManagerCode = ManagerCodeEntry.Text;

            if (string.IsNullOrWhiteSpace(enteredManagerCode))
            {
                await DisplayAlert("������", "������� ��� ���������.", "OK");
                return;
            }

            if (enteredManagerCode == ManagerAccessCode)
            {
                Preferences.Set("AuthorizationType", "Manager");
                Preferences.Set("IsAuthorized", true);
                NavigateToMainPage();
                StartBackgroundProductLoading(); // ������ ������� ��������
            }
            else
            {
                await DisplayAlert("������", "�������� ��� ������� ���������.", "OK");
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

        private void StartBackgroundProductLoading()
        {
            // ������ ������� ������ ��� �������� ������
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("�������� ������ � ��������� � ������� ������...");
                    await LoadAllProductsFromUrl();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"������ ��� �������� ������: {ex.Message}");
                }
            });
        }

        private async Task LoadAllProductsFromUrl()
        {
            string fileUrl = await GetFileDownloadLinkFromYandex();
            if (string.IsNullOrEmpty(fileUrl))
            {
                Console.WriteLine("�� ������� �������� ������ ��� ����������.");
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
                    Console.WriteLine("������ ������� ��������� � �������.");
                    var productGroups = JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(content);
                    if (productGroups != null)
                    {
                        foreach (var group in productGroups)
                        {
                            ProductCache.CachedProducts[group.Key] = group.Value;
                        }
                        ProductCache.SaveCache();
                        Console.WriteLine("������ ������� ��������� � ���.");
                    }
                }
                else
                {
                    Console.WriteLine("������ ��������: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��������: {ex.Message}");
            }
        }

        private async Task<string> GetFileDownloadLinkFromYandex()
        {
            string yandexFilePath = "/productsCOLOR.json";  // ���� � ����� �� ������.�����
            string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";  // OAuth �����

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
                Console.WriteLine("������ ��������� ������ ��� ����������: " + response.StatusCode);
                return null;
            }
        }
    }
}