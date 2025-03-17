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
        public static TaskCompletionSource<bool> DatabaseLoadedTcs = new TaskCompletionSource<bool>();
        public bool ReturnToMainMenuAfterAddingProduct { get; set; } = false;

        public App()
        {
            InitializeComponent();
            InitializeDatabaseAsync();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Console.WriteLine($"[App] Необработанное исключение: {args.ExceptionObject}");
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                Console.WriteLine($"[App] Необработанное исключение: {exception?.Message}");
                Console.WriteLine($"[App] InnerException: {exception?.InnerException?.Message}");
                Console.WriteLine($"[App] StackTrace: {exception?.StackTrace}");
            };
            LoadProductsOnStartup();
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                Console.WriteLine("[App] Инициализация базы данных...");

                // Reset the TaskCompletionSource
                DatabaseLoadedTcs = new TaskCompletionSource<bool>();

                // Check network connectivity
                var current = Connectivity.Current;
                if (current.NetworkAccess != NetworkAccess.Internet)
                {
                    Console.WriteLine("[App] Нет подключения к интернету. Загрузка базы данных отложена.");
                    DatabaseLoadedTcs.TrySetResult(false);
                    return;
                }

                // First check if we already have cached products
                if (ProductCache.CachedProducts != null && ProductCache.CachedProducts.Count > 0)
                {
                    Console.WriteLine("[App] Продукты уже загружены из кэша.");
                    AppState.IsDatabaseLoaded = true;
                    DatabaseLoadedTcs.TrySetResult(true);
                    return;
                }

                // Try loading from local database
                var databaseService = new DatabaseService();
                var productGroups = await databaseService.LoadProductGroupsAsync();

                if (productGroups != null && productGroups.Count > 0)
                {
                    Console.WriteLine("[App] Продукты загружены из локальной базы данных.");
                    ProductCache.CachedProducts = productGroups;
                    AppState.IsDatabaseLoaded = true;
                    DatabaseLoadedTcs.TrySetResult(true);
                    return;
                }

                // If database is empty, try loading from Yandex Disk
                Console.WriteLine("[App] База данных пуста, пробуем загрузить с Яндекс Диска...");
                string fileUrl = await GetFileDownloadLinkFromYandex();

                if (string.IsNullOrEmpty(fileUrl))
                {
                    Console.WriteLine("[App] Не удалось получить ссылку для скачивания.");
                    DatabaseLoadedTcs.TrySetResult(false);
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
                        Console.WriteLine("[App] Данные успешно загружены с Яндекс Диска.");
                        var loadedProductGroups = JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(content);

                        if (loadedProductGroups != null && loadedProductGroups.Count > 0)
                        {
                            ProductCache.CachedProducts = loadedProductGroups;
                            ProductCache.SaveCache();

                            // Save the data to the database for future use
                            await databaseService.SaveProductGroupsAsync(loadedProductGroups);

                            AppState.IsDatabaseLoaded = true;
                            DatabaseLoadedTcs.TrySetResult(true);
                            Console.WriteLine("[App] Данные сохранены в кэш и базу данных.");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[App] Ошибка загрузки с Яндекс Диска: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Ошибка при загрузке с Яндекс Диска: {ex.Message}");
                }

                Console.WriteLine("[App] Не удалось загрузить базу данных. Результат пустой.");
                DatabaseLoadedTcs.TrySetResult(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Ошибка при инициализации базы данных: {ex.Message}");
                DatabaseLoadedTcs.TrySetResult(false);
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
    public static class AppState
{
    public static bool IsDatabaseLoaded { get; set; } = false;
}
}
