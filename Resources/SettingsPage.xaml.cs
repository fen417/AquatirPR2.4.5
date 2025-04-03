using Microsoft.Maui.Storage;
using System.Net.Mail;
using System.Net;
using Aquatir.Model;

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
            ShowProductImagesSwitch.IsToggled = Preferences.Get("ShowProductImages", false); // По умолчанию выключено
            // Подписка на события переключателей
            AutoReturnSwitch.Toggled += OnAutoReturnToggled;
            IgnoreColorsSwitch.Toggled += OnIgnoreColorsToggled;
            //  ShowPriceSwitch.Toggled += OnShowPriceToggled;
            ShowPackagedProductsSwitch.Toggled += OnShowPackagedProductsToggled;
            ShowProductImagesSwitch.Toggled += OnShowProductImagesToggled;
        }

        private void OnAutoReturnToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("AutoReturnEnabled", e.Value);
        }
        private void OnShowProductImagesToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("ShowProductImages", e.Value);
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

        private async void OnWhatsNewClicked(object sender, EventArgs e)
        {
            string patchNotes = @"
Что нового в версии r3.1.0:
- Добавлена возможность включить в настройках отображение упаковки продукции

Что нового в версии r3.0.0:
- Добавлена редактор списка продукции (для менеджеров)
- Обновлён интерфейс главной страницы, страниц истории и деталей заказа, страницы настроек

Что нового в версии r2.8.2:
- Добавлено отображение комментария к заказу в истории заказов

Что нового в версии r2.8.1:
- Добавлена поддержка уведомлений
- Добавлена поддержка обратной связи
- Исправлены ошибки

Что нового в версии r2.8.0:
- Существенная оптимизация скорости загрузки групп при помощи внедрения базы данных
- Добавлен чекбокс 'доп. заявка'
- Исправлены сбои программы при потери интернет-соединения или во время звонков

Что нового в версии r2.7.0:
- Добавлена группа 'Вся продукция', содержащая полный список продукции
- Улучшения интерфейса 

Что нового в версии r2.6.2:
- Исправления ошибок и оптимизация приложения

Что нового в версии r2.6.1:
- Изменён и оптимизирован способ загрузки списка продукции

Что нового в версии r2.6.0:
- Добавлено push-уведомление с напоминанием о заказе горячего копчения (каждый ПН в 17:00)
- Улучшена производительность при загрузке больших списков товаров
- Исправлены мелкие ошибки и улучшена стабильность приложения
- Улучшен интерфейс настроек

Что нового в версии r2.5.3:
- Добавлена проверка даты заказа
- Оптимизирован код
- Исправлены мелкие ошибки (Hotfix #1)
- Исправлены мелкие ошибки (Hotfix #2)

Что нового в версии r2.5.0:
- Исправлена сортировка продукции, содержащей цветовые теги
- Исправлено суммирование продукции в заказе и предпросмотре
";
            await DisplayAlert("Что нового?", patchNotes, "ОК");
        }

        private async void OnReportIssueClicked(object sender, EventArgs e)
        {
            string userInput = await DisplayPromptAsync(
                "Сообщение об ошибке или предложение",
                "Пожалуйста, опишите обнаруженную ошибку или предложите улучшение:",
                "Отправить",
                "Отмена",
                placeholder: "Введите текст сообщения");

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                await SendFeedbackEmail(userInput);
            }
        }

        private async Task SendFeedbackEmail(string feedbackText)
        {
            try
            {
                var message = new MailMessage();
                message.From = new MailAddress("rep.1958@mail.ru", "Aquatir App");
                message.To.Add(new MailAddress("rep.1958@mail.ru", "Поддержка Aquatir"));
                message.Subject = "Обратная связь по приложению Aquatir";
                message.Body = $"Обратная связь от пользователя приложения:\n\n{feedbackText}\n\n3.1.0";

                using (var client = new SmtpClient("smtp.mail.ru", 587)) // Изменен порт на 587
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false; // Явно указываем не использовать учетные данные по умолчанию
                    client.Credentials = new NetworkCredential("rep.1958@mail.ru", "zyxrhkQb4KwE0Udwz2cx");
                    client.Timeout = 10000; // Установка таймаута в 10 секунд

                    try
                    {
                        // Используем асинхронный метод SendMailAsync вместо Task.Run
                        await client.SendMailAsync(message);
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await DisplayAlert("Успешно", "Ваше сообщение успешно отправлено. Спасибо за обратную связь!", "OK"));
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await DisplayAlert("Ошибка", $"Не удалось отправить сообщение: {ex.Message}", "OK"));
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK"));
            }
        }

        // Добавлен метод для открытия страницы редактора продукции с проверкой пароля
        private async void OnProductEditorClicked(object sender, EventArgs e)
        {
            // Запрашиваем пароль
            string password = await DisplayPromptAsync(
                "Требуется авторизация",
                "Введите пароль для доступа к редактору продукции:",
                "Войти",
                "Отмена",
                placeholder: "Введите пароль",
                maxLength: 20,
                keyboard: Keyboard.Numeric);

            // Проверяем пароль
            if (password == "160400")
            {
                // Открываем страницу редактора продукции
                await Navigation.PushAsync(new ProductEditorPage());
            }
            else if (!string.IsNullOrEmpty(password))
            {
                // Показываем сообщение об ошибке, если пароль неверный
                await DisplayAlert("Ошибка", "Неверный пароль. Доступ запрещен.", "OK");
            }
        }
    }
}