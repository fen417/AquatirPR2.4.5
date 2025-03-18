using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Aquatir.Model;

namespace Aquatir
{
    public class RemoteNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _yandexFilePath;
        private readonly string _oauthToken;

        public RemoteNotificationService(string oauthToken = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q")
        {
            _httpClient = new HttpClient();
            _yandexFilePath = "/notifications.json"; // Путь к файлу на вашем Яндекс.Диске
            _oauthToken = oauthToken;
        }

        public async Task<List<RemoteNotification>> GetNotificationsAsync()
        {
            try
            {
                // Получаем ссылку для скачивания файла
                string downloadLink = await GetFileDownloadLinkFromYandex();

                if (string.IsNullOrEmpty(downloadLink))
                {
                    Console.WriteLine($"[RemoteNotificationService] Не удалось получить ссылку для скачивания файла");
                    return new List<RemoteNotification>();
                }

                // Скачиваем содержимое файла
                _httpClient.DefaultRequestHeaders.Clear(); // Сбрасываем заголовки перед скачиванием
                var response = await _httpClient.GetAsync(downloadLink);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                // Проверяем, что получили JSON, а не HTML
                if (content.TrimStart().StartsWith("<"))
                {
                    throw new FormatException("Получен HTML вместо JSON");
                }

                // Десериализуем JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<RemoteNotification>>(content, options)
                    ?? new List<RemoteNotification>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[RemoteNotificationService] Ошибка HTTP запроса: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[RemoteNotificationService] Ошибка десериализации JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteNotificationService] Непредвиденная ошибка: {ex.Message}");
            }

            return new List<RemoteNotification>();
        }

        private async Task<string> GetFileDownloadLinkFromYandex()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {_oauthToken}");

                var response = await _httpClient.GetAsync(
                    $"https://cloud-api.yandex.net/v1/disk/resources/download?path={Uri.EscapeDataString(_yandexFilePath)}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Используем System.Text.Json вместо Newtonsoft.Json
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    return jsonResponse.GetProperty("href").GetString();
                }
                else
                {
                    Console.WriteLine($"[RemoteNotificationService] Ошибка получения ссылки для скачивания: {response.StatusCode}");
                    // Можно вывести тело ответа для отладки
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[RemoteNotificationService] Сообщение об ошибке: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteNotificationService] Ошибка при получении ссылки для скачивания: {ex.Message}");
                return null;
            }
        }
    }
}