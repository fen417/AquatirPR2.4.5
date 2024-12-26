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


namespace Aquatir
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();
        private Order _currentOrder = new Order();
        private Dictionary<string, List<string>> _customersByDirection;
        public bool ShowCityPicker { get; set; }
        public bool IsManager { get; set; }
        public MainPage()
        {
            InitializeComponent();
            bool isAuthorizationDisabled = Preferences.Get("AuthorizationDisabled", false);
            Preferences.Set("AuthorizationDisabled", false); // ЗАМЕНИТЬ НА FALSE ДЛЯ ВКЛЮЧЕНИЯ АВТОРИЗАЦИИ
            if (!Preferences.ContainsKey("ShowPackagedProducts"))
            {
                Preferences.Set("ShowPackagedProducts", true);
            }
            bool isShowPackagedProductsEnabled = Preferences.Get("ShowPackagedProducts", true);
            IsManager = Preferences.Get("AuthorizationType", string.Empty) == "Manager";
            if (isAuthorizationDisabled)
            {
                ShowCityPicker = true;
            }
            else
            {
                string authType = Preferences.Get("AuthorizationType", "User");
                ShowCityPicker = authType == "Manager";
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

                    // Сравниваем в нижнем регистре
                    if (validShops.Contains(userShopName.ToLower()))
                    {
                        OrderDatePicker.Date = DateTime.Now.AddDays(1);
                    }
                    else
                    {
                        OrderDatePicker.Date = DateTime.Now;
                    }
                }
            }
            BindingContext = this;

        _customersByDirection = new Dictionary<string, List<string>>
            {
                { "Тирасполь", new List<string> {
                    "Агора Бородино", 
                    "Агора Зелинского",
                    "Агора Краснодонская",
                    "Агора Юности",
                    "Агора Чкалова",
                    "БМК - 2",
                    "БМК - 31",
                    "Динисалл Каховская",
                    "Динисалл Краснодонская",
                    "Динисалл Палома",
                    "Динисалл Фортуна",
                    "И/П Насиковский",
                    "И/П Сырбул",
                    "И/П Хаджи",
                    "И/П Кобзарь",
                    "ООО Наполи (р-н Джорджия)",
                    "Сигл Комета",
                    "Сигл Ларионова",
                    "У Семёныча",
                    "Фагот",
                    "Эверест",
                    "Эндис"
                    }
                },
                { "Бендеры", new List<string> {
                    "Агора Гиска",
                    "БМК - 4",
                    "БМК - 9",
                    "БМК - 24",
                    "БМК - 29",
                    "БМК - 30",
                    "Динисалл Победы",
                    "Динисалл Салют",
                    "Динисалл Социальный",
                    "И/П Балан",
                    "И/П Белая",
                    "И/П Березина",
                    "И/П Грек",
                    "И/П Новойдарская",
                    "И/П Новойдарский",
                    "И/П Эдигер",
                    "Ильина",
                    "Шишкин",
                    "ЮГ 1",
                    "ЮГ 2",
                    "ЮГ 3",
                    "ЮГ 4",
                    "ЮГ 5",
                    "ЮГ 7",
                    "ЮГ 8"
                } },
                { "Григориополь", new List<string> {
                    "Дарануца",
                } },
                { "Самовывоз", new List<string> {
                    "",
                } }
            };

            OrdersCollectionView.ItemsSource = _orders;
        }
        private void OnPrivatePersonCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                CustomerNameEntry.Text = CustomerNameEntry.Text + " на частное лицо";
            }
            else
            {
                if (CustomerNameEntry.Text.Contains(" на частное лицо"))
                {
                    CustomerNameEntry.Text = CustomerNameEntry.Text.Replace(" на частное лицо", string.Empty);
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
       

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Preferences.ContainsKey("CurrentOrder"))
            {
                string currentOrderJson = Preferences.Get("CurrentOrder", string.Empty);
                if (!string.IsNullOrWhiteSpace(currentOrderJson))
                {
                    try
                    {
                        var restoredOrder = JsonConvert.DeserializeObject<Order>(currentOrderJson);

                        if (string.IsNullOrWhiteSpace(CustomerNameEntry.Text))
                        {
                            CustomerNameEntry.Text = restoredOrder.CustomerName;
                        }

                        if (DirectionPicker.SelectedItem == null)
                        {
                            DirectionPicker.SelectedItem = restoredOrder.Direction;
                        }

                        OrderDatePicker.Date = restoredOrder.OrderDate;
                        CommentEntry.Text = restoredOrder.Comment;
                        _currentOrder = restoredOrder;
                        PreviewCollectionView.ItemsSource = _currentOrder.Products
                            .Select(p => FormatProductString(p))
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        _currentOrder = new Order();
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CustomerNameEntry.Text))
                {
                    CustomerNameEntry.Text = Preferences.Get("UserShopName", string.Empty);
                }

                if (DirectionPicker.SelectedItem == null)
                {
                    DirectionPicker.SelectedItem = Preferences.Get("UserCity", string.Empty);
                }
            }
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

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
                // Проверка, существует ли уже продукт в заказе
                var existingProduct = _currentOrder.Products
                    .FirstOrDefault(p => p.Name == productName);

                if (existingProduct != null)
                {
                    // Если продукт найден, суммируем количество
                    existingProduct.Quantity += quantity;
                }
                else
                {
                    // Если продукта нет, добавляем новый
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

                UpdatePreview(); // Обновляем превью
                SaveCurrentOrder(); // Сохраняем заказ
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

        private decimal GetProductPrice(string productName, string priceType)
        {
            foreach (var group in ProductCache.CachedProducts)
            {
                var product = group.Value.FirstOrDefault(p => p.Name == productName);
                if (product != null)
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
            }
            return 0;
        }

        private async void OnGroupButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string groupName = button.Text;

            await Navigation.PushAsync(new ProductSelectionPage(groupName, this));
        }
       
        private async void OnFinishOrderClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CustomerNameEntry.Text) || !_currentOrder.Products.Any())
            {
                await DisplayAlert("Ошибка", "Пожалуйста, заполните все поля.", "OK");
                return;
            }

            string customerName = CustomerNameEntry.Text.Trim();

            if (customerName.Equals("Акватир Григориополь", StringComparison.OrdinalIgnoreCase))
            {
                customerName = "И/П Дарануца";
            }
            else if (customerName.Equals("Акватир Бендеры", StringComparison.OrdinalIgnoreCase))
            {
                customerName = "И/П Гончаренко";
            }
            else if (customerName.Equals("Акватории Григориополь", StringComparison.OrdinalIgnoreCase))
            {
                customerName = "И/П Дарануца";
            }
            else if (customerName.Equals("ИП - Романова", StringComparison.OrdinalIgnoreCase))
            {
                customerName = "И/П Романова";
            }

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

        private void SendOrdersByEmail(List<Order> selectedOrders)
        {
            var groupOrder = ProductCache.CachedProducts.Keys.ToList();
            var ordersByDate = selectedOrders.GroupBy(o => o.OrderDate).ToList();
            foreach (var orderGroup in ordersByDate)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Aquatir", "rep.1958@mail.ru"));
                message.To.Add(new MailboxAddress("Получатель", "fen559256@gmail.com"));

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
                    .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase);


                    foreach (var product in sortedProducts)
                    {
                        bodyText += $"<div> <font size='3'>{product.DisplayName} - {product.DisplayQuantity}</font></div>";
                    }

                    if (!string.IsNullOrWhiteSpace(order.Comment))
                    {
                        bodyText += $"<div><br/><font size='3'>Комментарий к заказу: <i>{order.Comment}</i></font></div>";
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

        private string RemoveColorTags(string input)
        {
            return Regex.Replace(input, @"<\/?color.*?>", string.Empty);
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

        private string FormatProductString(ProductItem product)
        {
            string productName = System.Text.RegularExpressions.Regex.Replace(product.Name, "<color.*?>|</color>", "").Trim();
            decimal quantity = product.Quantity;
            string unit = productName.EndsWith("ВЕС.") ? "кг." :
                           productName.EndsWith("УП.") ? "уп." :
                           productName.EndsWith("ШТ.") ? "шт." :
                           productName.EndsWith("В.") ? "в." :
                           productName.EndsWith("КОНТ.") ? "конт." : "";

            foreach (var unitString in new[] { "УП.", "ВЕС.", "ШТ.", "В.", "КОНТ." })
            {
                productName = productName.Replace(unitString, "").Trim();
            }

            string formattedQuantity = quantity % 1 == 0 ? quantity.ToString("0") : quantity.ToString("0.0#");
            bool showPrice = Preferences.Get("ShowPriceEnabled", false);
            string priceInfo = "";

            if (unit == "уп." && product.PricePerUnit > 0)
            {
                priceInfo = showPrice ? $" ({quantity * product.PricePerUnit:N2} руб.)" : "";
            }
            else if (unit == "кг." && product.PricePerKg > 0)
            {
                priceInfo = showPrice ? $" ({quantity * product.PricePerKg:N2} руб.)" : "";
            }
            else if (unit == "конт." && product.PricePerCont > 0)
            {
                priceInfo = showPrice ? $" ({quantity * product.PricePerCont:N2} руб.)" : "";
            }
            else if (unit == "шт." && product.PricePerPiece > 0)
            {
                priceInfo = showPrice ? $" ({quantity * product.PricePerPiece:N2} руб.)" : "";
            }
            else if (unit == "в." && product.PricePerVedro > 0)
            {
                priceInfo = showPrice ? $" ({quantity * product.PricePerVedro:N2} руб.)" : "";
            }

            return $"{productName} - {formattedQuantity} {unit}{priceInfo}";
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
        private async void OnOrderHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OrderHistoryPage());
        }
        private void UpdatePreview()
        {
            var productDescriptions = _currentOrder.Products.Select(p => FormatProductString(p)).ToList();
            decimal totalAmount = 0;
            foreach (var product in _currentOrder.Products)
            {
                if (product.Name.EndsWith("ВЕС."))
                {
                    totalAmount += product.Quantity * product.PricePerKg;
                }
                else if (product.Name.EndsWith("УП."))
                {
                    totalAmount += product.Quantity * product.PricePerUnit;
                }
                else if (product.Name.EndsWith("КОНТ."))
                {
                    totalAmount += product.Quantity * product.PricePerCont;
                }
                else if (product.Name.EndsWith("ШТ."))
                {
                    totalAmount += product.Quantity * product.PricePerPiece;
                }
                else if (product.Name.EndsWith("В."))
                {
                    totalAmount += product.Quantity * product.PricePerVedro;
                }
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
    }
}