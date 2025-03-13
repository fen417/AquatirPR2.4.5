using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Aquatir
{
    public partial class App : Application
    {
        private static readonly HttpClient client = new HttpClient();

        public bool ReturnToMainMenuAfterAddingProduct { get; set; } = false;

        public App()
        {
            InitializeComponent();
            InitializeDatabaseAsync();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Console.WriteLine($"[App] Необработанное исключение: {args.ExceptionObject}");
            };
            LoadProductsOnStartup();
        }
        
        private async void InitializeDatabaseAsync()
    {
        try
        {
            Console.WriteLine("[App] Инициализация базы данных...");
            var databaseService = new DatabaseService();
            var productGroups = await databaseService.LoadProductGroupsAsync();
            ProductCache.CachedProducts = productGroups;
            AppState.IsDatabaseLoaded = true;
            Console.WriteLine("[App] База данных инициализирована успешно.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[App] Ошибка при инициализации базы данных: {ex.Message}");
            // В случае ошибки инициализации БД приложение попытается загрузить её позже
        }
    }

        private async void LoadProductsOnStartup()
        {
            try
            {
                bool isAuthorized = Preferences.Get("IsAuthorized", false);
                if (isAuthorized)
                {
                    await LoadAllProductsFromUrl();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Ошибка при загрузке продуктов: {ex.Message}");
            }
        }

        private async Task LoadAllProductsFromUrl()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Console.WriteLine("Нет подключения к интернету.");
                return;
            }

            string fileUrl = await GetFileDownloadLinkFromYandex();
            if (string.IsNullOrEmpty(fileUrl))
            {
                Console.WriteLine("Не удалось получить ссылку для скачивания.");
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
                    Console.WriteLine("Данные успешно загружены с сервера.");
                    var productGroups = JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(content);
                    if (productGroups != null)
                    {
                        foreach (var group in productGroups)
                        {
                            ProductCache.CachedProducts[group.Key] = group.Value;
                        }
                        ProductCache.SaveCache();
                        Console.WriteLine("Данные успешно сохранены в кэш.");
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка загрузки: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async Task<string> GetFileDownloadLinkFromYandex()
        {
            try
            {
                string yandexFilePath = "/productsCOLOR.json";  // Путь к файлу на Яндекс.Диске
                string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";  // OAuth токен

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
                    Console.WriteLine("Ошибка получения ссылки для скачивания: " + response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении ссылки для скачивания: {ex.Message}");
                return null;
            }
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            bool isAuthorized = Preferences.Get("IsAuthorized", false);

            Page startPage = isAuthorized ? new AppShell() : new NavigationPage(new PasswordPage());

            return new Window(startPage);
        }
    }

    // Класс ProductCache
    public static class ProductCache
    {
        private const string CacheKey = "CachedProducts";
        private const string LastModifiedKey = "LastModified";

        public static Dictionary<string, List<ProductItem>> CachedProducts { get; set; } = LoadCache();

        public static DateTime LastModified { get; set; } = LoadLastModified();

        public static void SaveCache()
        {
            try
            {
                var json = JsonConvert.SerializeObject(CachedProducts ?? new Dictionary<string, List<ProductItem>>());
                Preferences.Set(CacheKey, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении кэша: {ex.Message}");
            }
        }

        public static void SaveLastModified(DateTime lastModified)
        {
            LastModified = lastModified;
            Preferences.Set(LastModifiedKey, lastModified.ToString("O"));
        }

        private static Dictionary<string, List<ProductItem>> LoadCache()
        {
            var json = Preferences.Get(CacheKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new Dictionary<string, List<ProductItem>>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(json)
                       ?? new Dictionary<string, List<ProductItem>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке кэша: {ex.Message}");
                return new Dictionary<string, List<ProductItem>>();
            }
        }

        private static DateTime LoadLastModified()
        {
            var lastModifiedString = Preferences.Get(LastModifiedKey, string.Empty);
            if (DateTime.TryParse(lastModifiedString, out DateTime lastModified))
                return lastModified;

            return DateTime.MinValue;
        }
    }
}
