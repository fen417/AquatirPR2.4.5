using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aquatir.Model;

namespace Aquatir
{
    public class NotificationManager
    {
        private const string ShownNotificationsKey = "ShownNotificationIds";
        private readonly RemoteNotificationService _notificationService;

        public NotificationManager()
        {
            _notificationService = new RemoteNotificationService();
        }

        public async Task CheckAndShowNotificationsAsync()
        {
            try
            {
                var current = Connectivity.Current;
                if (current.NetworkAccess != NetworkAccess.Internet)
                {
                    Console.WriteLine("[NotificationManager] Нет подключения к интернету. Проверка уведомлений отменена.");
                    return;
                }

                var notifications = await _notificationService.GetNotificationsAsync();
                var shownIds = GetShownNotificationIds();

                foreach (var notification in notifications)
                {
                    // Проверяем, показывалось ли уже это уведомление
                    if (!string.IsNullOrEmpty(notification.Id) && !shownIds.Contains(notification.Id))
                    {
                        await ShowNotificationAsync(notification);

                        // Добавляем ID в список показанных уведомлений
                        shownIds.Add(notification.Id);
                        SaveShownNotificationIds(shownIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationManager] Ошибка при проверке уведомлений: {ex.Message}");
            }
        }

        private async Task ShowNotificationAsync(RemoteNotification notification)
        {
            await Application.Current.MainPage.DisplayAlert(
                notification.Title,
                notification.Message,
                "OK");
        }

        private HashSet<string> GetShownNotificationIds()
        {
            string savedIds = Preferences.Get(ShownNotificationsKey, string.Empty);
            if (string.IsNullOrEmpty(savedIds))
            {
                return new HashSet<string>();
            }

            return new HashSet<string>(savedIds.Split(','));
        }

        private void SaveShownNotificationIds(HashSet<string> ids)
        {
            string idsString = string.Join(",", ids);
            Preferences.Set(ShownNotificationsKey, idsString);
        }
    }
}