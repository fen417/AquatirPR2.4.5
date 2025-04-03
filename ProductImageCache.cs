using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
namespace Aquatir
{
    public static class ProductImageCache
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly Dictionary<string, string> ImageUrls = new Dictionary<string, string>();
        private static bool isInitialized = false;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private static readonly Dictionary<string, string> DirectImageLinks = new Dictionary<string, string>();
        private static readonly Dictionary<string, DateTime> ImageLinkExpiration = new Dictionary<string, DateTime>();

        // Время жизни прямой ссылки Яндекс Диска (обычно 24 часа, но для надежности устанавливаем меньше)
        private static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(8);

        public static async Task InitializeAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (isInitialized)
                    return;
                string imageListUrl = await GetFileDownloadLinkFromYandex();
                if (!string.IsNullOrEmpty(imageListUrl))
                {
                    var response = await client.GetAsync(imageListUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var imageDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                        if (imageDict != null)
                        {
                            foreach (var item in imageDict)
                            {
                                // Store both normalized and original keys
                                string normalizedKey = NormalizeProductName(item.Key);
                                ImageUrls[item.Key] = item.Value;

                                // Add normalized version as alternative key if different
                                if (normalizedKey != item.Key && !ImageUrls.ContainsKey(normalizedKey))
                                {
                                    ImageUrls[normalizedKey] = item.Value;
                                }
                            }
                            isInitialized = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing image cache: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static async Task<string> GetImageUrlForProduct(string productName)
        {
            if (!isInitialized)
                await InitializeAsync();

            // Try with original name first
            if (ImageUrls.TryGetValue(productName, out string yandexLink1))
            {
                return await GetOrCreateDirectLink(yandexLink1);
            }

            // If not found, try with normalized name
            string normalizedName = NormalizeProductName(productName);
            if (ImageUrls.TryGetValue(normalizedName, out string yandexLink2))
            {
                return await GetOrCreateDirectLink(yandexLink2);
            }

            // Debug logging
            Console.WriteLine($"[DEBUG] Product not found: '{productName}', normalized: '{normalizedName}'");
            Console.WriteLine($"[DEBUG] Available keys: {string.Join(", ", ImageUrls.Keys)}");

            return null; // No image found for this product
        }

        private static async Task<string> GetOrCreateDirectLink(string yandexLink)
        {
            // If ссылка уже преобразована в прямую и не истекла, возвращаем её
            if (DirectImageLinks.TryGetValue(yandexLink, out string directLink))
            {
                if (ImageLinkExpiration.TryGetValue(yandexLink, out DateTime expirationTime) &&
                    DateTime.Now < expirationTime)
                {
                    return directLink;
                }
                // Если ссылка истекла, удаляем её из кэша
                DirectImageLinks.Remove(yandexLink);
                ImageLinkExpiration.Remove(yandexLink);
            }

            // Получаем прямую ссылку на скачивание через API
            string directDownloadLink = await GetDirectYandexDiskLink(yandexLink);
            if (!string.IsNullOrEmpty(directDownloadLink))
            {
                DirectImageLinks[yandexLink] = directDownloadLink;
                ImageLinkExpiration[yandexLink] = DateTime.Now.Add(LinkLifetime);
                return directDownloadLink;
            }

            return null;
        }

        private static async Task<string> GetDirectYandexDiskLink(string publicLink)
        {
            try
            {
                // Создаем запрос к API для получения прямой ссылки на скачивание
                var response = await client.GetAsync($"https://cloud-api.yandex.net/v1/disk/public/resources/download?public_key={Uri.EscapeDataString(publicLink)}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<dynamic>(content);
                    return json.href.ToString(); // Прямая ссылка для скачивания
                }
                else
                {
                    Console.WriteLine("Error getting Yandex Disk download link: " + response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Yandex Disk direct link: {ex.Message}");
                return null;
            }
        }

        private static string NormalizeProductName(string productName)
        {
            // Remove color tags and trim the name
            string normalizedName = System.Text.RegularExpressions.Regex.Replace(productName, @"<\/?color.*?>", string.Empty).Trim();
            // Remove "УП." suffix if present
            if (normalizedName.EndsWith("УП.", StringComparison.OrdinalIgnoreCase))
                normalizedName = normalizedName.Substring(0, normalizedName.Length - 3).Trim();
            return normalizedName;
        }

        private static async Task<string> GetFileDownloadLinkFromYandex()
        {
            string yandexFilePath = "/productImages.json";  // Path to the JSON file with image URLs
            string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";  // OAuth token
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
                Console.WriteLine("Error getting download link: " + response.StatusCode);
                return null;
            }
        }
    }
}