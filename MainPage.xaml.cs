using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Plugin.LocalNotification;


namespace Aquatir
{
    public partial class MainPage : ContentPage
    {
        private readonly ISpeechToTextService _speechToTextService; // Измените тип
        private ObservableCollection<ProductItem> _allAvailableProducts;
        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();
        private Order _currentOrder = new Order();
        private Dictionary<string, List<string>> _customersByDirection;
        public bool IsDataLoaded { get; private set; } = false;
        public bool ShowCityPicker { get; set; }
        public bool IsManager { get; set; }
        public bool IsNotManager => !IsManager;

        public MainPage(ISpeechToTextService speechToTextService)
        {
            InitializeComponent();
            _speechToTextService = speechToTextService;
            OrderDatePicker.Date = DateTime.Today;
            Connectivity.Current.ConnectivityChanged += Current_ConnectivityChanged;
            LoadAllProductsForVoiceRecognition();
#if ANDROID
            ScheduleWeeklyNotification();
            CheckMissedNotification();
#endif
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
            "галион", "акватир бендеры", "И/П Дарануца", "ИП Романова"
        };

                OrderDatePicker.Date = validShops.Contains(userShopName.ToLower()) ? DateTime.Now.AddDays(1) : DateTime.Now;
            }

            BindingContext = this;

            _customersByDirection = new Dictionary<string, List<string>>
    {
        { "Тирасполь", new List<string> {
            "Агора Бородино", "Агора Зелинского", "Агора Краснодонская", "Агора Сакриера", "Агора Юности", "Агора Чкалова",
            "БМК - 2", "БМК - 31", "Динисалл Каховская", "Динисалл Краснодонская", "Динисалл Палома",
            "Динисалл Фортуна", "И/П Насиковский", "И/П Онищенко", "И/П Сырбул", "И/П Хаджи", "И/П Кобзарь",
            "ООО Миатита", "ООО Наполи (р-н Джорджия)", "Сигл Комета", "Сигл Ларионова", "У Семёныча", "Фагот",
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
        }

        private async void LoadAllProductsForVoiceRecognition()
        {
            try
            {
               
                if (ProductCache.CachedProducts == null || ProductCache.CachedProducts.Count == 0)
                {
                   
                    using var stream = await FileSystem.OpenAppPackageFileAsync("productsCOLOR.json");
                    using var reader = new StreamReader(stream);
                    var jsonContent = await reader.ReadToEndAsync();
                    ProductCache.CachedProducts = JsonConvert.DeserializeObject<Dictionary<string, List<ProductItem>>>(jsonContent);
                }

                _allAvailableProducts = new ObservableCollection<ProductItem>();
                foreach (var group in ProductCache.CachedProducts.Values)
                {
                    foreach (var product in group)
                    {
                        product.NormalizedNameForSearch = NormalizeTextForSearch(product.Name, isProductNameFromList: true);
                        _allAvailableProducts.Add(product);
                    }
                }
                IsDataLoaded = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка загрузки данных", $"Не удалось загрузить данные о продуктах для голосового распознавания: {ex.Message}", "OK");
                IsDataLoaded = false;
            }
        }
        private string NormalizeTextForSearch(string text, bool isProductNameFromList = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalizedText = text.ToLowerInvariant();

            normalizedText = Regex.Replace(normalizedText, @"\bхк\b", "х/к");
            normalizedText = Regex.Replace(normalizedText, @"\bгк\b", "г/к");
            normalizedText = Regex.Replace(normalizedText, @"\bслабосол\b|\bслабосоленая\b", "с/с");
            normalizedText = Regex.Replace(normalizedText, @"\bспецпосол\b|\bспец\b", "сп/п");
            normalizedText = Regex.Replace(normalizedText, @"\bпряная\b|\bпрянп\b", "п/п");
            normalizedText = normalizedText
                .Replace("пресервы", "пр-вы")
                .Replace("пресерв", "пр-вы")
                .Replace("сельдь", "с-дь")
                .Replace("скумбрия", "ск.")
                .Replace("скумбр", "ск.")
                .Replace("осётр", "ос.")
                .Replace("осетр", "ос.")
                .Replace("лосось", "лос.")
                .Replace("сёмга", "семга")  
                .Replace("семга", "сёмга"); 

            if (isProductNameFromList)
            {
                normalizedText = normalizedText
                    .Replace("вес.", "кг")
                    .Replace("уп.", "уп")
                    .Replace("шт.", "шт")
                    .Replace("в.", "в")
                    .Replace("конт.", "конт");
            }
            else
            {
                normalizedText = normalizedText
                    .Replace("килограмма", "кг")
                    .Replace("килограмм", "кг")
                    .Replace("кило", "кг")
                    .Replace("штука", "шт")
                    .Replace("штуки", "шт")
                    .Replace("штук", "шт")
                    .Replace("ведро", "в")
                    .Replace("ведра", "в")
                    .Replace("вёдер", "в")
                    .Replace("упаковка", "уп")
                    .Replace("упаковки", "уп")
                    .Replace("упаковок", "уп")
                    .Replace("коробки", "уп")
                    .Replace("коробик", "уп")
                    .Replace("контейнер", "конт")
                    .Replace("контейнера", "конт")
                    .Replace("контейнеров", "конт");

                normalizedText = Regex.Replace(normalizedText, @"\b\d{2}\s*/\s*\d{2}\b", "");
                normalizedText = Regex.Replace(normalizedText, @"\b\d{2}\s+на\s+\d{2}\b", "");
                normalizedText = Regex.Replace(normalizedText, @"\b\d{2}\s+\d{2}\b", "");
                normalizedText = Regex.Replace(normalizedText, @"\b[0-9]+([.,][0-9]+)?\s*(кг|грамм|гр|л)\b", "");
                normalizedText = Regex.Replace(normalizedText, @"\bрыба\b|\bхочу\b|\bмне\b|\bдобавь\b|\bпожалуйста\b", "");
            }

            normalizedText = Regex.Replace(normalizedText, @"<\/?color.*?>", string.Empty);
            normalizedText = Regex.Replace(normalizedText, @"\s+", " ").Trim();

            return normalizedText;
        }


        private async void OnVoiceInputClicked(object sender, EventArgs e)
        {
            if (!IsDataLoaded)
            {
                await DisplayAlert("Ошибка", "Данные о продуктах еще не загружены. Пожалуйста, подождите.", "OK");
                return;
            }

            try
            {
                var isGranted = await _speechToTextService.RequestPermissions();
                if (!isGranted)
                {
                    await DisplayAlert("Ошибка", "Разрешение на использование микрофона не предоставлено.", "OK");
                    return;
                }

                SpeechIndicator.IsVisible = true;
                SpeechIndicator.Text = "Слушаю..."; 
                                                    

                await DisplayAlert("Голосовой ввод", "Говорите, чтобы добавить продукты. Например: 'я хочу кильку хк 2 кило, ведро мойвы спец посола, 10 кг мороженого хека'", "OK");

                string recognizedText = string.Empty;
                try
                {
                    recognizedText = await _speechToTextService.ListenAsync(
                        CultureInfo.GetCultureInfo("ru-RU"),
                        CancellationToken.None
                    );
                }
                catch (OperationCanceledException)
                {
                    await DisplayAlert("Голосовой ввод", "Распознавание отменено.", "OK");
                    return;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Ошибка распознавания речи: {ex.Message}", "OK");
                    return;
                }
                finally
                {
                    SpeechIndicator.IsVisible = false;
                }


                if (!string.IsNullOrWhiteSpace(recognizedText))
                {
                    ParseAndAddProducts(recognizedText);
                }
                else
                {
                    await DisplayAlert("Голосовой ввод", "Ничего не было распознано. Попробуйте еще раз.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Произошла ошибка при голосовом вводе: {ex.Message}", "OK");
            }
        }
        private void ParseAndAddProducts(string voiceInput)
        {
            Debug.WriteLine($"Исходный голосовой ввод: {voiceInput}");

            // Нормализуем весь входной текст сразу, чтобы привести его к единому формату для дальнейшего парсинга
            string normalizedInput = NormalizeTextForSearch(voiceInput, isProductNameFromList: false);
            Debug.WriteLine($"Нормализованный ввод для парсинга: {normalizedInput}");

            var productEntries = new List<(string Name, decimal Quantity, string Unit)>();

            // Обновленное регулярное выражение для более гибкого парсинга
            // Оно ищет [число] [единица] [название] ИЛИ [название] [число] [единица]
            // (.+?) - нежадный захват любого текста (названия продукта)
            // (\d+(?:[.,]\d+)?) - число с опциональной дробной частью
            // (кг|шт|уп|в|конт) - список допустимых единиц
            string pattern = @"(?:(\d+(?:[.,]\d+)?)\s*(кг|шт|уп|в|конт)\s*(.+?)(?=\s*(?:\d+(?:[.,]\d+)?\s*(?:кг|шт|уп|в|конт)|и|,|$))?)|(?:(.+?)\s*(\d+(?:[.,]\d+)?)\s*(кг|шт|уп|в|конт)(?=\s*(?:\d+(?:[.,]\d+)?\s*(?:кг|шт|уп|в|конт)|и|,|$)))";

            var matches = Regex.Matches(normalizedInput, pattern, RegexOptions.IgnoreCase);

            if (matches.Count == 0)
            {
                // Если нет полных совпадений с количеством и единицей, попробуем найти только название
                // Это может быть случай "килька х/к" без количества, тогда по умолчанию будет 1
                var simpleProductMatches = Regex.Matches(normalizedInput, @"(.+?)(?=\s*и|,|$)", RegexOptions.IgnoreCase);
                foreach (Match simpleMatch in simpleProductMatches)
                {
                    string simpleProductName = simpleMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(simpleProductName))
                    {
                        // Ищем продукт с учетом нормализации
                        var foundProduct = FindBestProductMatch(simpleProductName, ""); // Пустая единица, т.к. ее не было в речи
                        if (foundProduct != null)
                        {
                            productEntries.Add((foundProduct.Name, 1m, ProductItem.GetUnitFromName(foundProduct.Name))); // По умолчанию 1 и официальная единица
                        }
                    }
                }
            }
            else
            {
                foreach (Match match in matches)
                {
                    string productNameRaw = string.Empty;
                    decimal quantity = 0;
                    string unitSpoken = string.Empty;

                    if (match.Groups[1].Success) // Quantity-Unit-Name pattern
                    {
                        quantity = decimal.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                        unitSpoken = match.Groups[2].Value;
                        productNameRaw = match.Groups[3].Value.Trim();
                    }
                    else if (match.Groups[4].Success) // Name-Quantity-Unit pattern
                    {
                        productNameRaw = match.Groups[4].Value.Trim();
                        quantity = decimal.Parse(match.Groups[5].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                        unitSpoken = match.Groups[6].Value;
                    }

                    // Удаляем единицу из productNameRaw, чтобы получить чистое название для поиска
                    string cleanedProductName = RemoveSpokenUnitFromProductName(productNameRaw, unitSpoken);

                    if (quantity > 0 && !string.IsNullOrWhiteSpace(cleanedProductName))
                    {
                        // Теперь ищем продукт используя чистое название и нормализованную единицу
                        var matchedProduct = FindBestProductMatch(cleanedProductName, unitSpoken);

                        if (matchedProduct != null)
                        {
                            productEntries.Add((matchedProduct.Name, quantity, ProductItem.GetUnitFromName(matchedProduct.Name)));
                        }
                        else
                        {
                            Debug.WriteLine($"Could not find a match for product: {cleanedProductName} with unit {unitSpoken}");
                            Dispatcher.Dispatch(async () =>
                            {
                                await DisplayAlert("Продукт не найден", $"Не удалось найти продукт: \"{cleanedProductName}\" с количеством \"{quantity} {unitSpoken}\".", "OK");
                            });
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Не удалось разобрать часть: {match.Value}");
                    }
                }
            }


            if (productEntries.Any())
            {
                foreach (var entry in productEntries)
                {
                    AddProductToOrder(entry.Name, entry.Quantity);
                }

                Dispatcher.Dispatch(async () =>
                {
                    await DisplayAlert("Успех", "Продукты были добавлены в заявку.", "OK");
                    UpdateOrderDisplay(); // Обновите отображение заказа
                });
            }
            else
            {
                Dispatcher.Dispatch(async () =>
                {
                    await DisplayAlert("Не удалось добавить", "Не удалось распознать продукты и количество из вашей речи. Пожалуйста, попробуйте еще раз.", "OK");
                });
            }
        }

        private string RemoveSpokenUnitFromProductName(string productName, string unit)
        {
            // Здесь unit уже нормализован (кг, уп, шт и т.д.)
            // Проверяем, заканчивается ли productName этим unit
            if (productName.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
            {
                return productName.Substring(0, productName.Length - unit.Length).Trim();
            }
            return productName;
        }

        // Helper to find the best product match
        private ProductItem FindBestProductMatch(string spokenProductNamePart, string spokenUnit)
        {
            // Нормализуем часть названия продукта для поиска, как из речи
            string normalizedSpokenProductNamePart = NormalizeTextForSearch(spokenProductNamePart, isProductNameFromList: false);

            // Нормализуем произнесенную единицу для сравнения с единицами в ProductItem.GetUnitFromName
            string normalizedSpokenUnit = spokenUnit.ToLower(); // кг, уп, шт и т.д.

            Debug.WriteLine($"Поиск: normalizedSpokenProductNamePart='{normalizedSpokenProductNamePart}', normalizedSpokenUnit='{normalizedSpokenUnit}'");

            ProductItem bestMatch = null;
            int bestScore = -1;

            foreach (var product in _allAvailableProducts)
            {
                if (string.IsNullOrEmpty(product.NormalizedNameForSearch)) continue;

                // Проверяем, содержит ли нормализованное имя продукта из списка нормализованную часть из речи
                if (product.NormalizedNameForSearch.Contains(normalizedSpokenProductNamePart))
                {
                    int currentScore = normalizedSpokenProductNamePart.Length; // Базовый счет за совпадение подстроки

                    // Проверяем соответствие единиц
                    string productOfficialUnit = ProductItem.GetUnitFromName(product.Name).ToLower(); // Получаем "кг", "уп", "шт" и т.д.

                    // Если произнесена единица, и она совпадает с официальной единицей продукта
                    if (!string.IsNullOrEmpty(normalizedSpokenUnit) && productOfficialUnit == normalizedSpokenUnit)
                    {
                        currentScore += 100; // Большой бонус за совпадение единицы
                    }
                    // Если единица не произнесена, но мы нашли продукт, и его единица является "кг", даем небольшой бонус, так как это часто подразумевается
                    else if (string.IsNullOrEmpty(normalizedSpokenUnit) && productOfficialUnit == "кг")
                    {
                        currentScore += 10;
                    }

                    // Если длина совпадения больше или счет лучше
                    if (currentScore > bestScore)
                    {
                        bestScore = currentScore;
                        bestMatch = product;
                    }
                }
            }
            return bestMatch;
        }

        // Этот метод будет вызван из ParseAndAddProducts, его содержание осталось без изменений, но убедитесь, что он есть.
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
                        // Используем DatabaseService для получения цены, если ProductItem не содержит напрямую.
                        // Или убедитесь, что ваш ProductItem имеет эти цены, загруженные из ProductCache
                        PricePerKg = productName.EndsWith("ВЕС.", StringComparison.OrdinalIgnoreCase) ? GetProductPrice(productName, "Kg") : 0,
                        PricePerUnit = productName.EndsWith("УП.", StringComparison.OrdinalIgnoreCase) ? GetProductPrice(productName, "Unit") : 0,
                        PricePerCont = productName.EndsWith("КОНТ.", StringComparison.OrdinalIgnoreCase) ? GetProductPrice(productName, "Cont") : 0,
                        PricePerPiece = productName.EndsWith("ШТ.", StringComparison.OrdinalIgnoreCase) ? GetProductPrice(productName, "Piece") : 0,
                        PricePerVedro = productName.EndsWith("В.", StringComparison.OrdinalIgnoreCase) ? GetProductPrice(productName, "Vedro") : 0
                    };
                    _currentOrder.Products.Add(productItem);
                }

                Preferences.Set("SelectedOrderDate", OrderDatePicker.Date.ToString("o"));

                UpdatePreview();
                SaveCurrentOrder();
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Произошла ошибка при добавлении продукта: {ex.Message}", "OK");
            }
        }
        private void UpdateOrderDisplay()
        {
            OrdersCollectionView.ItemsSource = null;
            OrdersCollectionView.ItemsSource = _orders;
            UpdatePreview(); // Обновить также и предпросмотр текущего заказа
        }


        // Helper to find the best product match

        private string RemoveUnitFromName(string productName)
        {
            // Эта функция должна быть согласована с тем, как единицы измерения сохраняются в ProductItem
            // Она удаляет суффиксы единиц измерения, чтобы получить "чистое" название продукта.
            string[] units = { "ВЕС.", "УП.", "ШТ.", "В.", "КОНТ." }; // Используем точные суффиксы из JSON
            string cleanedName = productName;
            foreach (var unit in units)
            {
                if (cleanedName.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    cleanedName = cleanedName.Substring(0, cleanedName.Length - unit.Length).Trim();
                    break;
                }
            }
            return RemoveColorTags(cleanedName); // Также удаляем теги цвета
        }

        // Helper to get official unit from spoken unit
        private string GetOfficialUnitFromSpokenUnit(string spokenUnit)
        {
            return spokenUnit.ToLower() switch
            {
                "кг" => "ВЕС.", // Assuming "ВЕС." for kilograms
                "уп" => "УП.",
                "шт" => "ШТ.",
                "в" => "В.",
                "конт" => "КОНТ.",
                _ => ""
            };
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
#if ANDROID
            LocalNotificationCenter.Current.Show(notification);
#endif
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
            Dispatcher.Dispatch(async () =>
            {

                if (e.NetworkAccess != NetworkAccess.Internet)
                {
                    await DisplayAlert("Предупреждение", "Интернет-соединение отсутствует. Некоторые функции могут быть недоступны.", "OK");
                }
                else
                {
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
                    return;
                }

                await Task.Run(() =>
                {
                    foreach (var group in ProductCache.CachedProducts)
                    {
                        if (group.Value.Any(product => product.IsNew && !HasProductBeenSeen(product.Name)))
                        {
                            // Используем Dispatcher для выполнения кода на основном потоке
                            Dispatcher.Dispatch(() =>
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
        _currentOrder.IsAdditionalOrder = true;
    }
    else
    {
        _currentOrder.IsAdditionalOrder = false;
    }
}
        private void SendOrdersByEmail(List<Order> selectedOrders)
        {
            var groupOrder = ProductCache.CachedProducts.Keys.ToList();
            var customerNames = selectedOrders.Select(o => o.CustomerName).Distinct();
            var allDates = selectedOrders.Select(o => o.OrderDate.ToString("dd.MM.yyyy")).Distinct();
            bool hasAdditionalOrder = selectedOrders.Any(o => o.IsAdditionalOrder);
            string additionalOrderText = hasAdditionalOrder ? " (доп. заявка)" : "";
            string orderDateText = string.Join(", ", allDates);

            // Обработка всех заказов
            var processedOrders = new List<ProcessedOrder>();

            foreach (var order in selectedOrders)
            {
                var regularProducts = new List<ProductItem>();
                var frozenProducts = new List<ProductItem>();
                var seafoodProducts = new List<ProductItem>();

                foreach (var product in order.Products)
                {
                    string productNameLower = product.Name.ToLower();

                    if (productNameLower.Contains("морожен") ||
                        productNameLower.Contains("ухи") ||
                        productNameLower.Contains("уха") ||
                        productNameLower.Contains("печень") ||
                        productNameLower.Contains("спецразделка"))
                    {
                        frozenProducts.Add(product);
                    }
                    else if (productNameLower.Contains("креветка") ||
                             productNameLower.Contains("кальмар") ||
                             productNameLower.Contains("мясо мидий") ||
                             productNameLower.Contains("alfredo") ||
                             productNameLower.Contains("морепродукты") ||
                             productNameLower.Contains("осьминог"))
                    {
                        seafoodProducts.Add(product);
                    }
                    else
                    {
                        regularProducts.Add(product);
                    }
                }

                processedOrders.Add(new ProcessedOrder
                {
                    OriginalOrder = order,
                    RegularProducts = regularProducts,
                    FrozenProducts = frozenProducts,
                    SeafoodProducts = seafoodProducts
                });
            }

            SendEmailForProductCategory(processedOrders, customerNames, orderDateText, additionalOrderText, "", groupOrder);
        }


        private void SendEmailForProductCategory(List<ProcessedOrder> processedOrders, IEnumerable<string> customerNames,
            string orderDateText, string additionalOrderText, string categoryTag, List<string> groupOrder)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Aquatir", "rep.1958@mail.ru"));
            message.To.Add(new MailboxAddress("Получатель", "fen559256@gmail.com"));

            message.Subject = $"Заявка от {string.Join(", ", customerNames)}{additionalOrderText} на {orderDateText}";

            var bodyBuilder = new BodyBuilder();

            foreach (var processedOrder in processedOrders)
            {
                var order = processedOrder.OriginalOrder;
                var directionText = !string.IsNullOrWhiteSpace(order.Direction) ? order.Direction : "Не указано";
                var orderDate = order.OrderDate.ToString("dd.MM.yyyy");

                var bodyText = $"<div><b><u><font size='5'>{order.CustomerName} ({directionText}).</font><font size='3'> Заявка на {orderDate}</font></u></b></div>";

                void AppendProductGroup(List<ProductItem> products, string groupTitle)
                {
                    if (products.Any())
                    {
                        if (!string.IsNullOrEmpty(groupTitle))
                        {
                            bodyText += $"<div><br/><b><font size='3'>{groupTitle}</font></b></div>";
                        }

                        var sorted = products
                            .OrderBy(product => GetProductGroupIndex(RemoveColorTags(product.Name), groupOrder))
                            .ThenBy(product => RemoveColorTags(product.Name), StringComparer.OrdinalIgnoreCase)
                            .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var hotSmoking = sorted.Where(p => p.Name.Contains("г/к", StringComparison.OrdinalIgnoreCase)).ToList();
                        var regular = sorted.Except(hotSmoking).ToList();

                        foreach (var p in regular)
                            bodyText += $"<div><font size='3'>{p.DisplayName} - {p.DisplayQuantity}</font></div>";

                        if (hotSmoking.Any())
                        {
                            bodyText += "<div><br/><b><font size='3'>Горячее копчение:</font></b></div>";
                            foreach (var p in hotSmoking)
                                bodyText += $"<div><font size='3'>{p.DisplayName} - {p.DisplayQuantity}</font></div>";
                        }
                    }
                }


                AppendProductGroup(processedOrder.RegularProducts, null);
                AppendProductGroup(processedOrder.FrozenProducts, "Мороженая продукция:");
                AppendProductGroup(processedOrder.SeafoodProducts, "Креветка:");

                if (!string.IsNullOrWhiteSpace(order.Comment))
                {
                    bodyText += $"<div><br/><font size='3'>Комментарий к заказу: <i>{order.Comment}</i></font></div>";
                }

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

        // Вспомогательный класс для обработки заказов
        private class ProcessedOrder
        {
            public Order OriginalOrder { get; set; }
            public List<ProductItem> RegularProducts { get; set; } = new List<ProductItem>();
            public List<ProductItem> FrozenProducts { get; set; } = new List<ProductItem>();
            public List<ProductItem> SeafoodProducts { get; set; } = new List<ProductItem>();
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
       
       
        private void RefreshPage()
        {
            OrdersCollectionView.ItemsSource = null;
            OrdersCollectionView.ItemsSource = _orders;
            UpdatePreview();
            UpdateGroupButtonsState();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var notificationManager = new NotificationManager();
            await notificationManager.CheckAndShowNotificationsAsync();
            IsDataLoaded = false;
            UpdateGroupButtonsState();

            try
            {
                // Если база данных уже загружена, то просто обновляем UI
                if (AppState.IsDatabaseLoaded && ProductCache.CachedProducts != null && ProductCache.CachedProducts.Count > 0)
                {
                    IsDataLoaded = true;
                }
                else
                {

                    // Дать шанс базе данных загрузиться из App.xaml.cs
                    bool isLoaded = await Task.WhenAny(App.DatabaseLoadedTcs.Task, Task.Delay(3000)) == App.DatabaseLoadedTcs.Task;

                    if (isLoaded && await App.DatabaseLoadedTcs.Task)
                    {
                        IsDataLoaded = true;
                    }
                    else
                    {
                        bool success = await ValidateProductCache();
                        IsDataLoaded = success;
                    }
                }

                await Task.Run(LoadSeenProducts);
                await CheckForNewProductsAsync();
                RefreshPage();
                RestoreCurrentOrder();
                UpdateGroupButtonsState();
            }
            catch (Exception ex)
            {
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
                    }
                    catch (Exception ex)
                    {
                        _currentOrder = new Order();
                    }
                }
            }
        }

        private async Task<bool> ValidateProductCache()
        {
            try
            {
                if (ProductCache.CachedProducts != null && ProductCache.CachedProducts.Count > 0)
                {
                    AppState.IsDatabaseLoaded = true; // Установим флаг, что данные загружены
                    return true;
                }
                else
                {
                    var current = Connectivity.Current;
                    if (current.NetworkAccess != NetworkAccess.Internet)
                    {
                        await DisplayAlert("Предупреждение", "Нет подключения к интернету. Продукты не загружены.", "OK");
                        return false;
                    }
                    else
                    {

                        var databaseService = new DatabaseService();
                        var productGroups = await databaseService.LoadProductGroupsAsync();

                        if (productGroups != null && productGroups.Count > 0)
                        {
                            ProductCache.CachedProducts = productGroups;
                            AppState.IsDatabaseLoaded = true;
                            return true;
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось загрузить продукты. Попробуйте ещё раз.", "OK");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
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
            var selectedDate = OrderDatePicker.Date;
            _currentOrder = new Order();
            OrderDatePicker.Date = selectedDate;
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

            return $"{productName} - {formattedQuantity} {unit}";
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
