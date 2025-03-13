using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;

namespace Aquatir
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();
        private Order _currentOrder = new Order();
        private Dictionary<string, List<string>> _customersByDirection;
        public bool IsDataLoaded { get; private set; } = false;
        public bool ShowCityPicker { get; set; }
        public bool IsManager { get; set; }
        public bool IsNotManager => !IsManager;
        public static class AppState
{
    public static bool IsDatabaseLoaded { get; set; } = false;
}

        public MainPage()
        {
            Console.WriteLine("[MainPage] Конструктор вызван.");
            InitializeComponent();
            Console.WriteLine("[MainPage] Инициализация компонентов завершена.");
            Connectivity.Current.ConnectivityChanged += Current_ConnectivityChanged;

            // Логирование инициализации данных
            Console.WriteLine("[MainPage] Инициализация данных...");
            ScheduleWeeklyNotification();
            CheckMissedNotification();
            bool isAuthorizationDisabled = Preferences.Get("AuthorizationDisabled", false);
            Preferences.Set("AuthorizationDisabled", false);

            if (!Preferences.ContainsKey("ShowPackagedProducts"))
            {
                Preferences.Set("ShowPackagedProducts", true);
            }

            IsManager = Preferences.Get("AuthorizationType", string.Empty) == "Manager";
            ShowCityPicker = isAuthorizationDisabled || IsManager;

            if (!ShowCityPicker)
            {
                string userCity = Preferences.Get("UserCity", string.Empty);
                string userShopName = Preferences.Get("UserShopName", string.Empty);

                if (!string.IsNullOrWhiteSpace(userCity))
                    DirectionPicker.SelectedItem = userCity;

                if (!string.IsNullOrWhiteSpace(userShopName))
                    CustomerNameEntry.Text = userShopName;

                var validShops = new List<string>
        {
            "ип - романова", "акватории григориополь", "акватир григориополь",
            "галион", "акватир бендеры"
        };

                OrderDatePicker.Date = validShops.Contains(userShopName.ToLower()) ? DateTime.Now.AddDays(1) : DateTime.Now;
            }

            BindingContext = this;

            _customersByDirection = new Dictionary<string, List<string>>
    {
        { "Тирасполь", new List<string> {
            "Агора Бородино", "Агора Зелинского", "Агора Краснодонская", "Агора Юности", "Агора Чкалова",
            "БМК - 2", "БМК - 31", "Динисалл Каховская", "Динисалл Краснодонская", "Динисалл Палома",
            "Динисалл Фортуна", "И/П Насиковский", "И/П Сырбул", "И/П Хаджи", "И/П Кобзарь",
            "ООО Наполи (р-н Джорджия)", "Сигл Комета", "Сигл Ларионова", "У Семёныча", "Фагот",
            "Эверест", "Эндис"
        }},
        { "Бендеры", new List<string> {
            "Агора Гиска", "БМК - 4", "БМК - 9", "БМК - 24", "БМК - 29", "БМК - 30",
            "Динисалл Победы", "Динисалл Салют", "Динисалл Социальный", "И/П Балан", "И/П Белая",
            "И/П Березина", "И/П Грек", "И/П Новойдарская", "И/П Новойдарский", "И/П Эдигер",
            "Ильина", "Шишкин", "ЮГ 1", "ЮГ 2", "ЮГ 3", "ЮГ 4", "ЮГ 5", "ЮГ 7", "ЮГ 8"
        }},
        { "Григориополь", new List<string> { "Дарануца" } },
        { "Самовывоз", new List<string> { "" } }
    };

            OrdersCollectionView.ItemsSource = _orders;
            Console.WriteLine("[MainPage] Инициализация данных завершена.");
        }

        private void ScheduleWeeklyNotification()
        {
            var notification = new NotificationRequest
            {
                NotificationId = 100,
                Title = "Напоминание",
                Description = "Завтра вторник, не забудьте заказать горячее копчение!",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = GetNextMondayAt5PM(),
                    RepeatType = NotificationRepeat.Weekly
                }
            };

            LocalNotificationCenter.Current.Show(notification);
        }

        private DateTime GetNextMondayAt5PM()
        {
            var now = DateTime.Now;
            var nextMonday = now.AddDays(((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7);
            return new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 17, 0, 0);
        }

        private void CheckMissedNotification()
        {
            var now = DateTime.Now;
            var lastNotificationDate = Preferences.Get("LastNotificationDate", DateTime.MinValue);

            if (now.DayOfWeek == DayOfWeek.Monday && lastNotificationDate.Date != now.Date)
            {
                ShowNotification();
                Preferences.Set("LastNotificationDate", now.Date);
            }
        }

        private void ShowNotification()
        {
            var notification = new NotificationRequest
            {
                NotificationId = 101,
                Title = "Напоминание",
                Description = "Завтра вторник, не забудьте заказать горячее копчение!",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(1)
                }
            };

            LocalNotificationCenter.Current.Show(notification);
        }

        private bool HasGroupBeenVisited(string groupName)
        {
            return Preferences.Get($"GroupVisited_{groupName}", false);
        }

        private void UpdateGroupBorderColor(Button button, string groupName)
        {
            if (groupName == "Вся продукция") return;

            if (HasNewProducts(groupName) && !HasGroupBeenVisited(groupName))
            {
                button.BorderColor = Colors.YellowGreen;
                button.BorderWidth = 4;
            }
            else
            {
                button.BorderColor = Colors.Transparent;
                button.BorderWidth = 0;
            }
        }
        private void Current_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
{
    Device.BeginInvokeOnMainThread(async () =>
    {
        if (e.NetworkAccess != NetworkAccess.Internet)
        {
            Console.WriteLine("[MainPage] Соединение с интернетом потеряно.");
            await DisplayAlert("Предупреждение", "Интернет-соединение отсутствует. Некоторые функции могут быть недоступны.", "OK");
        }
        else
        {
            Console.WriteLine("[MainPage] Соединение с интернетом восстановлено.");
            // Можно добавить повторную загрузку данных
        }
    });
}

        private void OnPrivatePersonCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                CustomerNameEntry.Text += " на частное лицо";
            }
            else
            {
                CustomerNameEntry.Text = CustomerNameEntry.Text.Replace(" на частное лицо", string.Empty);
            }
        }

        private void MarkAllProductsInGroupAsSeen(string groupName)
        {
            if (ProductCache.CachedProducts.ContainsKey(groupName))
            {
                foreach (var product in ProductCache.CachedProducts[groupName])
                {
                    if (product.IsNew)
                    {
                        MarkProductAsSeen(product.Name);
                    }
                }
            }
        }

        public void ReloadProducts()
        {
            if (Application.Current.MainPage.Navigation.NavigationStack.LastOrDefault() is ProductSelectionPage productSelectionPage)
            {
                productSelectionPage.LoadProducts();
            }
            else
            {
                Console.WriteLine("ProductSelectionPage не активен. Невозможно обновить продукты.");
            }
        }

        private async void OnAllProductsButtonClicked(object sender, EventArgs e)
        {
            Preferences.Set("SelectedOrderDate", OrderDatePicker.Date.ToString("o"));
            await Navigation.PushAsync(new ProductSelectionPage("Вся продукция", this));
        }

        private void OnDirectionChanged(object sender, EventArgs e)
        {
            if (DirectionPicker == null)
            {
                throw new InvalidOperationException("DirectionPicker is not initialized.");
            }

            string selectedDirection = DirectionPicker.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedDirection) &&
                _customersByDirection != null &&
                _customersByDirection.ContainsKey(selectedDirection))
            {
                if (CustomerPicker != null)
                {
                    CustomerPicker.ItemsSource = _customersByDirection[selectedDirection];
                }
            }
        }

        private void MarkProductAsSeen(string productName)
        {
            Preferences.Set($"ProductSeen_{productName}", true);
        }

        private bool HasProductBeenSeen(string productName)
        {
            return Preferences.Get($"ProductSeen_{productName}", false);
        }

        private bool HasNewProducts(string groupName)
        {
            if (ProductCache.CachedProducts.ContainsKey(groupName))
            {
                return ProductCache.CachedProducts[groupName]
                    .Any(product => product.IsNew && !HasProductBeenSeen(product.Name));
            }
            return false;
        }

        public void SaveSelectedOrderDate()
        {
            Preferences.Set("SelectedOrderDate", OrderDatePicker.Date.ToString("o"));
        }

        private async Task CheckForNewProductsAsync()
{
    try
    {
        // Проверяем доступность сети перед выполнением операции
        var current = Connectivity.Current;
        if (current.NetworkAccess != NetworkAccess.Internet)
        {
            Console.WriteLine("[MainPage] Нет подключения к интернету. Проверка новых продуктов отменена.");
            return;
        }
        
        await Task.Run(() =>
        {
            foreach (var group in ProductCache.CachedProducts)
            {
                if (group.Value.Any(product => product.IsNew && !HasProductBeenSeen(product.Name)))
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var button = GroupButtonsStackLayout.Children
                            .OfType<Button>()
                            .FirstOrDefault(b => b.Text == group.Key);
                        if (button != null)
                        {
                            UpdateGroupBorderColor(button, group.Key);
                        }
                    });
                }
            }
        });
    }
    catch (Exception ex)
    {
        // Логируем ошибку, но не пробрасываем её дальше
        Console.WriteLine($"[MainPage] Ошибка при проверке новых продуктов: {ex.Message}");
    }
}
        private async void OnSendOrdersClicked(object sender, EventArgs e)
        {
            var selectedOrders = OrdersCollectionView.SelectedItems.Cast<Order>().ToList();

            if (!selectedOrders.Any())
            {
                await DisplayAlert("Ошибка", "Выберите заказы для отправки.", "OK");
                return;
            }

            var current = Connectivity.Current;
            if (current.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("Ошибка", "Нет подключения к интернету. Заказы не могут быть отправлены.", "OK");
                return;
            }

            try
            {
                SendOrdersByEmail(selectedOrders);
                foreach (var order in selectedOrders)
                {
                    _orders.Remove(order);
                }

                OrdersCollectionView.SelectedItems.Clear();

                await DisplayAlert("Успех", "Заказы успешно отправлены на почту.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось отправить заказы: {ex.Message}", "OK");
            }
        }
private void OnAdditionalOrderCheckedChanged(object sender, CheckedChangedEventArgs e)
{
    if (e.Value)
    {
        // Если чекбокс активирован, добавляем текст "доп. заявка" в письмо
        _currentOrder.IsAdditionalOrder = true;
    }
    else
    {
        // Если чекбокс деактивирован, убираем текст "доп. заявка" из письма
        _currentOrder.IsAdditionalOrder = false;
    }
}
        private void SendOrdersByEmail(List<Order> selectedOrders)
{
    var groupOrder = ProductCache.CachedProducts.Keys.ToList();
    var ordersByDate = selectedOrders.GroupBy(o => o.OrderDate).ToList();

    foreach (var orderGroup in ordersByDate)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Aquatir", "rep.1958@mail.ru"));
        message.To.Add(new MailboxAddress("Получатель", "andreypir16@gmail.com"));

        var customerNames = orderGroup.Select(o => o.CustomerName).Distinct();
        string orderDateText = orderGroup.Key.ToString("dd.MM.yyyy");

        message.Subject = $"Заявка от {string.Join(", ", customerNames)} на {orderDateText}";

        var bodyBuilder = new BodyBuilder();

        foreach (var order in orderGroup)
        {
            var directionText = !string.IsNullOrWhiteSpace(order.Direction) ? order.Direction : "Не указано";
            var bodyText = $"<div><b><u><font size='5'>{order.CustomerName} ({directionText}).</font><font size='3'> Заявка на {orderDateText}</font></u></b></div>";

            var sortedProducts = order.Products
                .OrderBy(product => GetProductGroupIndex(RemoveColorTags(product.Name), groupOrder))
                .ThenBy(product => RemoveColorTags(product.Name), StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Разделение на основной список и горячее копчение
            var hotSmokingProducts = sortedProducts
                .Where(p => p.Name.Contains("г/к", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var regularProducts = sortedProducts.Except(hotSmokingProducts).ToList();

            // Формируем основной список продуктов
            foreach (var product in regularProducts)
            {
                bodyText += $"<div> <font size='3'>{product.DisplayName} - {product.DisplayQuantity}</font></div>";
            }

            // Добавляем горячее копчение отдельным блоком
            if (hotSmokingProducts.Any())
            {
                bodyText += "<div><br/><b> <font size='3'>Горячее копчение:</font></b></div>";
                foreach (var product in hotSmokingProducts)
                {
                    bodyText += $"<div> <font size='3'>{product.DisplayName} - {product.DisplayQuantity}</font></div>";
                }
            }

            if (!string.IsNullOrWhiteSpace(order.Comment))
            {
                bodyText += $"<div><br/><font size='3'>Комментарий к заказу: <i>{order.Comment}</i></font></div>";
            }

            // Добавляем "доп. заявка" в письмо, если чекбокс активирован
            if (order.IsAdditionalOrder)
            {
                bodyText += "<div><br/><font size='3'><b>доп. заявка</b></font></div>";
            }

            bodyText += "<div><br/></div>";

            bodyBuilder.HtmlBody += bodyText;
        }

        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            try
            {
                client.Connect("smtp.mail.ru", 465, true);
                client.Authenticate("rep.1958@mail.ru", "zyxrhkQb4KwE0Udwz2cx");

                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось отправить заказы: {ex.Message}", "OK");
            }
        }
    }
}

        private int GetProductGroupIndex(string productName, List<string> groupOrder)
        {
            foreach (var group in groupOrder)
            {
                if (ProductCache.CachedProducts[group]
                    .Any(product => RemoveColorTags(product.Name) == productName))
                {
                    return groupOrder.IndexOf(group);
                }
            }
            return int.MaxValue;
        }

        private HashSet<string> _seenProducts;

        private void LoadSeenProducts()
        {
            var allKeys = PreferenceHelper.GetAllKeys();
            _seenProducts = new HashSet<string>(
                allKeys
                    .Where(key => key.StartsWith("ProductSeen_"))
                    .Select(key => key.Substring("ProductSeen_".Length))
            );
        }
       
        private async Task ReloadProductCacheAsync()
{
    // Проверяем, были ли данные уже загружены
    if (AppState.IsDatabaseLoaded && ProductCache.CachedProducts.Count > 0)
    {
        Console.WriteLine("[MainPage] Кэш продукции уже загружен, пропускаем перезагрузку.");
        return;
    }

    Console.WriteLine("[MainPage] Перезагрузка кэша продукции...");
    try
    {
        // Проверяем доступность сети
        var current = Connectivity.Current;
        if (current.NetworkAccess != NetworkAccess.Internet)
        {
            Console.WriteLine("[MainPage] Нет подключения к интернету. Перезагрузка кэша отменена.");
            return;
        }
        
        var databaseService = new DatabaseService();
        var productGroups = await databaseService.LoadProductGroupsAsync();

        if (productGroups == null || productGroups.Count == 0)
        {
            Device.BeginInvokeOnMainThread(() =>
                Console.WriteLine("[MainPage] CRITICAL: Продукты не загружены!")
            );
            return;
        }

        ProductCache.CachedProducts = productGroups;
        AppState.IsDatabaseLoaded = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MainPage] Ошибка при перезагрузке кэша продукции: {ex.Message}");
    }
}

        protected override async void OnAppearing()
{
    base.OnAppearing();
    Console.WriteLine("[MainPage] Страница отображается.");

    // Блокируем кнопки групп
    IsDataLoaded = false;
    UpdateGroupButtonsState();

    try
    {
        // Загружаем данные только если они не были загружены в App.xaml.cs
        if (!AppState.IsDatabaseLoaded)
        {
            Console.WriteLine("[MainPage] База данных не была загружена ранее, загружаем...");
            // Пытаемся загрузить данные, если они не были загружены при запуске
            await ValidateProductCache();
        }
        else
        {
            Console.WriteLine("[MainPage] База данных уже загружена, пропускаем загрузку.");
        }
        
        // Эти операции выполняем всегда
        await Task.Run(LoadSeenProducts);
        await CheckForNewProductsAsync();
        RestoreSelectedDate();
        RestoreCurrentOrder();

        // Разблокируем кнопки групп после загрузки данных
        IsDataLoaded = true;
        UpdateGroupButtonsState();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MainPage] Ошибка при загрузке данных: {ex.Message}");
        await DisplayAlert("Предупреждение", "Не удалось загрузить данные из-за проблем с сетью. Некоторые функции могут быть недоступны.", "OK");
        IsDataLoaded = true;
        UpdateGroupButtonsState();
    }
}
        private void UpdateGroupButtonsState()
        {
            foreach (var button in GroupButtonsStackLayout.Children.OfType<Button>())
            {
                button.IsEnabled = IsDataLoaded;
            }
        }

        private void RestoreSelectedDate()
        {
            if (Preferences.ContainsKey("SelectedOrderDate"))
            {
                string savedDate = Preferences.Get("SelectedOrderDate", DateTime.Now.ToString("o"));
                if (DateTime.TryParse(savedDate, null, DateTimeStyles.RoundtripKind, out DateTime restoredDate))
                {
                    OrderDatePicker.Date = restoredDate;
                    Console.WriteLine($"[MainPage] Дата восстановлена: {restoredDate}");
                }
            }
        }

        private void RestoreCurrentOrder()
        {
            if (Preferences.ContainsKey("CurrentOrder"))
            {
                string currentOrderJson = Preferences.Get("CurrentOrder", string.Empty);
                if (!string.IsNullOrWhiteSpace(currentOrderJson))
                {
                    try
                    {
                        var restoredOrder = JsonConvert.DeserializeObject<Order>(currentOrderJson);
                        _currentOrder = restoredOrder;
                        Console.WriteLine($"[MainPage] Заказ восстановлен: {restoredOrder.OrderID}");
                    }
                    catch (Exception ex)
                    {
                        _currentOrder = new Order();
                        Console.WriteLine($"[MainPage] Ошибка при восстановлении заказа: {ex.Message}");
                    }
                }
            }
        }

        private async Task ValidateProductCache()
{
    try
    {
        if (ProductCache.CachedProducts != null && ProductCache.CachedProducts.Count > 0)
        {
            Console.WriteLine("[MainPage] Продукция загружена");
            AppState.IsDatabaseLoaded = true; // Установим флаг, что данные загружены
        }
        else
        {
            var current = Connectivity.Current;
            if (current.NetworkAccess != NetworkAccess.Internet)
            {
                Console.WriteLine("[MainPage] Нет подключения к интернету. Продукция не загружена.");
                await DisplayAlert("Предупреждение", "Нет подключения к интернету. Продукты не загружены.", "OK");
            }
            else
            {
                Console.WriteLine("[MainPage] Продукция не загружена, загружаем...");
                
                // Если данные не были загружены в App.xaml.cs, загружаем их здесь
                var databaseService = new DatabaseService();
                var productGroups = await databaseService.LoadProductGroupsAsync();
                
                if (productGroups != null && productGroups.Count > 0)
                {
                    ProductCache.CachedProducts = productGroups;
                    AppState.IsDatabaseLoaded = true;
                    Console.WriteLine("[MainPage] Продукция успешно загружена");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось загрузить продукты. Попробуйте ещё раз.", "OK");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MainPage] Ошибка при проверке кэша продукции: {ex.Message}");
    }
}
        private async Task LoadDataAsync()
{
    // Проверяем, были ли данные уже загружены
    if (AppState.IsDatabaseLoaded && ProductCache.CachedProducts.Count > 0)
    {
        Console.WriteLine("[MainPage] Данные уже загружены, пропускаем загрузку.");
        return;
    }

    Console.WriteLine("[MainPage] Загрузка данных...");
    try
    {
        // Проверяем доступность сети
        var current = Connectivity.Current;
        if (current.NetworkAccess != NetworkAccess.Internet)
        {
            Console.WriteLine("[MainPage] Нет подключения к интернету. Загрузка отменена.");
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var databaseService = new DatabaseService();
        var productGroups = await databaseService.LoadProductGroupsAsync().WaitAsync(cts.Token);

        Console.WriteLine($"[MainPage] Загружено {productGroups.Count} групп продукции.");
        AppState.IsDatabaseLoaded = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MainPage] Ошибка при загрузке данных: {ex.Message}");
    }
}

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Connectivity.Current.ConnectivityChanged -= Current_ConnectivityChanged;

            try
            {
                SaveCurrentOrder();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении текущего заказа: {ex.Message}");
            }
        }

        private void OnCustomerSelected(object sender, EventArgs e)
        {
            if (CustomerPicker.SelectedIndex != -1)
            {
                CustomerNameEntry.Text = CustomerPicker.SelectedItem.ToString();
            }

            UpdateCommentEntryState();
        }

        private void OnCustomerNameEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCommentEntryState();
        }

        private void UpdateCommentEntryState()
        {
            if (CommentEntry != null)
            {
                CommentEntry.IsEnabled = !string.IsNullOrWhiteSpace(CustomerNameEntry.Text);
            }
        }

        public void AddProductToOrder(string productName, decimal quantity)
        {
            try
            {
                var existingProduct = _currentOrder.Products
                    .FirstOrDefault(p => p.Name == productName);

                if (existingProduct != null)
                {
                    existingProduct.Quantity += quantity;
                }
                else
                {
                    var productItem = new ProductItem
                    {
                        Name = productName,
                        Quantity = quantity,
                        PricePerKg = productName.EndsWith("ВЕС.") ? GetProductPrice(productName, "Kg") : 0,
                        PricePerUnit = productName.EndsWith("УП.") ? GetProductPrice(productName, "Unit") : 0,
                        PricePerCont = productName.EndsWith("КОНТ.") ? GetProductPrice(productName, "Cont") : 0,
                        PricePerPiece = productName.EndsWith("ШТ.") ? GetProductPrice(productName, "Piece") : 0,
                        PricePerVedro = productName.EndsWith("В.") ? GetProductPrice(productName, "Vedro") : 0
                    };

                    _currentOrder.Products.Add(productItem);
                }

                Preferences.Set("SelectedOrderDate", OrderDatePicker.Date.ToString("o"));
                Console.WriteLine($"Product added: {productName}, quantity: {quantity}");
                Console.WriteLine($"Date saved: {OrderDatePicker.Date}");

                UpdatePreview();
                SaveCurrentOrder();
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Произошла ошибка при добавлении продукта: {ex.Message}", "OK");
            }
        }

        private void SaveCurrentOrder()
        {
            if (_currentOrder == null) return;

            if (_currentOrder.Products.Any())
            {
                string currentOrderJson = JsonConvert.SerializeObject(_currentOrder);
                Preferences.Set("CurrentOrder", currentOrderJson);
            }
            else
            {
                Preferences.Remove("CurrentOrder");
            }
        }

        private static readonly Dictionary<string, ProductItem> _productPriceCache = new Dictionary<string, ProductItem>();

        private decimal GetProductPrice(string productName, string priceType)
        {
            if (_productPriceCache.TryGetValue(productName, out var product))
            {
                return priceType switch
                {
                    "Kg" => product.PricePerKg,
                    "Unit" => product.PricePerUnit,
                    "Cont" => product.PricePerCont,
                    "Piece" => product.PricePerPiece,
                    "Vedro" => product.PricePerVedro,
                    _ => 0
                };
            }

            foreach (var group in ProductCache.CachedProducts)
            {
                var foundProduct = group.Value.FirstOrDefault(p => p.Name == productName);
                if (foundProduct != null)
                {
                    _productPriceCache[productName] = foundProduct;
                    return priceType switch
                    {
                        "Kg" => foundProduct.PricePerKg,
                        "Unit" => foundProduct.PricePerUnit,
                        "Cont" => foundProduct.PricePerCont,
                        "Piece" => foundProduct.PricePerPiece,
                        "Vedro" => foundProduct.PricePerVedro,
                        _ => 0
                    };
                }
            }
            return 0;
        }

        private async void OnGroupButtonClicked(object sender, EventArgs e)
        {
            if (!IsDataLoaded)
            {
                await DisplayAlert("Ошибка", "Данные ещё не загружены. Пожалуйста, подождите.", "OK");
                return;
            }

            var button = sender as Button;
            string groupName = button.Text;

            MarkAllProductsInGroupAsSeen(groupName);
            UpdateGroupBorderColor(button, groupName);

            Preferences.Set("SelectedOrderDate", OrderDatePicker.Date.ToString("o"));
            await Navigation.PushAsync(new ProductSelectionPage(groupName, this));
        }

        private string NormalizeCustomerName(string inputName)
        {
            var customerNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Акватир Григориополь", "И/П Дарануца" },
                { "Акватории Григориополь", "И/П Дарануца" },
                { "Гончаренко", "И/П Гончаренко" },
                { "ИП - Романова", "И/П Романова" },
                { "Галион", "Галион" }
            };

            string trimmedName = inputName.Trim();
            return customerNameMap.TryGetValue(trimmedName, out string normalizedName)
                ? normalizedName
                : trimmedName;
        }

        private async void OnFinishOrderClicked(object sender, EventArgs e)
        {
            if (OrderDatePicker.Date < DateTime.Today)
            {
                await DisplayAlert("Ошибка", "Дата заказа не может быть в прошлом. Пожалуйста, выберите корректную дату.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerNameEntry.Text) || !_currentOrder.Products.Any())
            {
                await DisplayAlert("Ошибка", "Пожалуйста, заполните все поля.", "OK");
                return;
            }

            string customerName = NormalizeCustomerName(CustomerNameEntry.Text);
            _currentOrder.CustomerName = customerName;
            _currentOrder.Direction = DirectionPicker.SelectedItem?.ToString();
            _currentOrder.OrderDate = OrderDatePicker.Date;
            _currentOrder.Comment = CommentEntry.Text;
            _currentOrder.CompletionDate = DateTime.Now;

            var orderHistoryService = new OrderHistoryService();
            var orderHistory = orderHistoryService.LoadOrderHistory();
            var existingOrder = orderHistory.Orders.FirstOrDefault(o => o.OrderID == _currentOrder.OrderID);
            if (existingOrder != null)
            {
                orderHistory.UpdateOrder(_currentOrder);
            }
            else
            {
                orderHistory.AddOrder(_currentOrder);
            }

            if (!_orders.Contains(_currentOrder))
            {
                _orders.Add(_currentOrder);
            }

            orderHistoryService.SaveOrderHistory(orderHistory);
            Preferences.Remove("CurrentOrder");
            _currentOrder = new Order();
            CommentEntry.Text = string.Empty;
            PreviewCollectionView.ItemsSource = null;
            CustomerPicker.SelectedIndex = -1;
            IsPrivatePersonCheckBox.IsChecked = false;

            if (CustomerNameEntry.Text.Contains(" на частное лицо"))
            {
                CustomerNameEntry.Text = CustomerNameEntry.Text.Replace(" на частное лицо", string.Empty);
            }

            string authType = Preferences.Get("AuthorizationType", "User");
            if (authType == "Manager")
            {
                CustomerNameEntry.Text = string.Empty;
            }

            bool isReminderShown = Preferences.Get("IsReminderShown", false);
            if (!isReminderShown)
            {
                await DisplayAlert("Напоминание", "Не забудьте отправить заказ, выбрав его из списка завершённых заказов и нажав 'Отправить выбранные заказы'.", "ОК");
                Preferences.Set("IsReminderShown", true);
            }
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Руководство по использованию",
                "1. Выберите дату на какую необходимо подготовить заявку\n" +
                "2. Выберите товары из групп и их количество\n" +
                "3. Завершите заказ, нажав кнопку 'Завершить заказ'\n" +
                "4. Выберите заказ/ы нажав по ним и нажмите кнопку 'Отправить выбранные заказы'\n\n" +
                "* Заказ можно отредактировать, нажав по нему, затем по кнопке 'Редактировать выбранный заказ'\n" +
                "** Отредактировать можно только один заказ за раз\n" +
                "*** Заказ/ы можно удалить, нажав по нему, затем по кнопке 'Удалить выбранные заказы'",
                "OK");
        }

        private void OnRemoveProductClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.CommandParameter is string productString)
            {
                var productToRemove = _currentOrder.Products
                    .FirstOrDefault(p => FormatProductString(p) == productString);

                if (productToRemove != null)
                {
                    _currentOrder.Products.Remove(productToRemove);
                    UpdatePreview();
                }
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        private async void OnOrderHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OrderHistoryPage());
        }

        private void UpdatePreview()
        {
            var productDescriptions = new List<string>();
            decimal totalAmount = 0;

            foreach (var product in _currentOrder.Products)
            {
                productDescriptions.Add(FormatProductString(product));

                decimal price = 0;
                if (product.Name.EndsWith("ВЕС.")) price = product.PricePerKg;
                else if (product.Name.EndsWith("УП.")) price = product.PricePerUnit;
                else if (product.Name.EndsWith("КОНТ.")) price = product.PricePerCont;
                else if (product.Name.EndsWith("ШТ.")) price = product.PricePerPiece;
                else if (product.Name.EndsWith("В.")) price = product.PricePerVedro;

                totalAmount += product.Quantity * price;
            }

            if (Preferences.Get("ShowPriceEnabled", false))
            {
                productDescriptions.Add($"Сумма заказа: {totalAmount:N2} руб.");
            }

            PreviewCollectionView.ItemsSource = productDescriptions;
        }

        public void RestoreOrderForEditing(Order order)
        {
            CustomerNameEntry.Text = order.CustomerName;
            DirectionPicker.SelectedItem = order.Direction;
            OrderDatePicker.Date = order.OrderDate;
            CommentEntry.Text = order.Comment;
            _currentOrder = order;
            UpdatePreview();
        }

        private string FormatProductString(ProductItem product)
        {
            string productName = RemoveColorTags(product.Name);

            var unitMapping = new Dictionary<string, string>
            {
                { "ВЕС.", "кг." },
                { "УП.", "уп." },
                { "ШТ.", "шт." },
                { "В.", "в." },
                { "КОНТ.", "конт." }
            };

            string unit = "";
            foreach (var suffix in unitMapping.Keys)
            {
                if (productName.EndsWith(suffix))
                {
                    unit = unitMapping[suffix];
                    productName = productName.Substring(0, productName.Length - suffix.Length).Trim();
                    break;
                }
            }

            string formattedQuantity = product.Quantity % 1 == 0
                ? product.Quantity.ToString("0")
                : product.Quantity.ToString("0.0#");

            string priceInfo = "";
            bool showPrice = Preferences.Get("ShowPriceEnabled", false);

            if (showPrice)
            {
                var priceMapping = new Dictionary<string, decimal>
                {
                    { "уп.", product.PricePerUnit },
                    { "кг.", product.PricePerKg },
                    { "конт.", product.PricePerCont },
                    { "шт.", product.PricePerPiece },
                    { "в.", product.PricePerVedro }
                };

                if (priceMapping.TryGetValue(unit, out decimal price) && price > 0)
                {
                    priceInfo = $" ({product.Quantity * price:N2} руб.)";
                }
            }

            return $"{productName} - {formattedQuantity} {unit}{priceInfo}";
        }

        private string RemoveColorTags(string input)
        {
            return Regex.Replace(input, @"<\/?color.*?>", string.Empty);
        }

        private void OnEditOrderClicked(object sender, EventArgs e)
        {
            var selectedOrders = OrdersCollectionView.SelectedItems.Cast<Order>().ToList();

            if (selectedOrders.Count == 0)
            {
                DisplayAlert("Ошибка", "Выберите один заказ для редактирования.", "OK");
                return;
            }
            else if (selectedOrders.Count > 1)
            {
                DisplayAlert("Ошибка", "Выберите только один заказ для редактирования.", "OK");
                return;
            }

            var selectedOrder = selectedOrders.First();
            CustomerNameEntry.Text = selectedOrder.CustomerName;
            DirectionPicker.SelectedItem = selectedOrder.Direction;
            OrderDatePicker.Date = selectedOrder.OrderDate;
            CommentEntry.Text = selectedOrder.Comment;
            _currentOrder = selectedOrder;
            UpdatePreview();
        }

        private void OnSaveEditedOrderClicked(object sender, EventArgs e)
        {
            if (_currentOrder == null)
            {
                DisplayAlert("Ошибка", "Нет текущего заказа для сохранения.", "OK");
                return;
            }

            _currentOrder.CustomerName = CustomerNameEntry.Text;
            _currentOrder.Direction = DirectionPicker.SelectedItem?.ToString();
            _currentOrder.OrderDate = OrderDatePicker.Date;
            _currentOrder.Comment = CommentEntry.Text;
            var orderIndex = _orders.IndexOf(_currentOrder);

            if (orderIndex != -1)
            {
                _orders[orderIndex] = new Order
                {
                    CustomerName = _currentOrder.CustomerName,
                    Direction = _currentOrder.Direction,
                    OrderDate = _currentOrder.OrderDate,
                    Products = _currentOrder.Products.ToList(),
                    Comment = _currentOrder.Comment
                };

                OrdersCollectionView.ItemsSource = null;
                OrdersCollectionView.ItemsSource = _orders;

                DisplayAlert("Успех", "Заказ успешно отредактирован.", "OK");
            }
            else
            {
                DisplayAlert("Ошибка", "Не удалось найти заказ для обновления.", "OK");
            }
        }

        private async void OnDeleteOrdersClicked(object sender, EventArgs e)
        {
            var selectedOrders = OrdersCollectionView.SelectedItems.Cast<Order>().ToList();

            if (!selectedOrders.Any())
            {
                await DisplayAlert("Ошибка", "Выберите заказы для удаления.", "OK");
                return;
            }
            bool confirm = await DisplayAlert("Подтверждение", "Вы уверены, что хотите удалить выбранные заказы?", "Да", "Нет");

            if (confirm)
            {
                foreach (var order in selectedOrders)
                {
                    _orders.Remove(order);
                }

                OrdersCollectionView.SelectedItems.Clear();
            }
        }
    }
}
