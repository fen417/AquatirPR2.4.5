using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Storage;

namespace Aquatir
{
    public class Order : INotifyPropertyChanged
    {
        private string _customerName = string.Empty;
        private string _direction = string.Empty;
        private DateTime _orderDate = DateTime.Now;
        private string _comment = string.Empty;
        private List<ProductItem> _products = new List<ProductItem>();
        public decimal TotalAmount
        {
            get
            {
                decimal total = 0;
                foreach (var product in Products)
                {
                    // Получаем единицу измерения продукта
                    string unit = GetUnitFromName(product.Name).ToLower();

                    // Определяем цену в зависимости от единицы измерения
                    decimal price = unit switch
                    {
                        "кг" => product.PricePerKg,
                        "уп." => product.PricePerUnit,
                        "шт." => product.PricePerPiece,
                        "конт." => product.PricePerCont,
                        "в." => product.PricePerVedro,
                        _ => 0
                    };

                    // Проверяем корректность данных
                    if (price > 0 && product.Quantity > 0)
                    {
                        decimal productTotal = product.Quantity * price;
                        Console.WriteLine($"Продукт: {product.Name}, Единица измерения: {unit}, Цена: {price}, Итого за продукт: {productTotal}");
                        total += productTotal;
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: Цена или количество для продукта {product.Name} некорректно");
                    }
                }

                // Проверка на случай, если итоговая сумма равна нулю
                if (total == 0)
                {
                    Console.WriteLine("Внимание: Итоговая сумма равна нулю. Проверьте данные продуктов.");
                }

                return total;
            }
        }

        public string FormattedTotalAmount
        {
            get
            {
                bool showPrice = Preferences.Get("ShowPriceEnabled", false);
                return showPrice ? $"Сумма заказа: {TotalAmount} руб." : string.Empty;
            }
        }
        public string FormattedOrderDetails
        {
            get
            {
                return $"{FormattedCompletionDate}\n{FormattedTotalAmount}";
            }
        }

        public string GetFormattedOrderSummary()
        {
            bool showPrice = Preferences.Get("ShowPriceEnabled", false);
            var orderSummary = new List<string>();

            foreach (var product in Products)
            {
                string unit = GetUnitFromName(product.Name);
                decimal price = unit switch
                {
                    "кг" => product.PricePerKg,
                    "уп." => product.PricePerUnit,
                    "шт." => product.PricePerPiece,
                    "конт." => product.PricePerCont,
                    "в." => product.PricePerVedro,
                    _ => 0
                };

                decimal totalProductPrice = product.Quantity * price;

                string productInfo = $"{product.Name} - {product.Quantity} {unit}";
                if (showPrice && price > 0)
                {
                    productInfo += $" ({totalProductPrice:F2} руб.)";
                }
                orderSummary.Add(productInfo);
            }

            if (showPrice)
            {
                orderSummary.Add($"Итого: {TotalAmount:F2} руб.");
            }

            return string.Join("\n", orderSummary);
        }

        private string GetUnitFromName(string productName)
        {
            if (productName.EndsWith("УП.", StringComparison.OrdinalIgnoreCase))
            {
                return "уп.";
            }
            else if (productName.EndsWith("ШТ.", StringComparison.OrdinalIgnoreCase))
            {
                return "шт.";
            }
            else if (productName.EndsWith("ВЕС.", StringComparison.OrdinalIgnoreCase))
            {
                return "кг";
            }
            else if (productName.EndsWith("В.", StringComparison.OrdinalIgnoreCase))
            {
                return "в.";
            }
            else if (productName.EndsWith("КОНТ.", StringComparison.OrdinalIgnoreCase))
            {
                return "конт.";
            }
            return string.Empty; // Или можно вернуть "ед." для незнакомых наименований
        }
        public string GetFormattedOrderSummaryForEmail()
        {
            var orderSummary = new List<string>();

            // Добавляем информацию о каждом продукте
            foreach (var product in Products)
            {
                // Убираем суффиксы и получаем чистое название продукта
                string productName = RemoveUnitFromName(product.Name).Replace("*", ""); ;

                // Получаем количество и форматируем его
                string formattedQuantity = product.Quantity % 1 == 0 ? product.Quantity.ToString("0") : product.Quantity.ToString("0.#");

                // Получаем единицу измерения
                string unit = GetUnitFromName(product.Name);

                // Формируем строку для отображения без стоимости
                string productInfo = $"{productName} - {formattedQuantity} {unit}";

                orderSummary.Add(productInfo);
            }

            // Возвращаем объединенные строки с тегами <br> для переноса
            return string.Join("<br>", orderSummary);
        }

        // Метод для удаления суффиксов из названия продукта
        private string RemoveUnitFromName(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return productName;

            string[] units = { "УП.", "ВЕС.", "ШТ.", "В.", "КОНТ." };
            foreach (var unit in units)
            {
                if (productName.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    return productName.Substring(0, productName.Length - unit.Length).Trim();
                }
            }
            return productName; // Если ни одно из условий не выполняется
        }

        // Команда для кнопки "Назад"
        public ICommand GoBackCommand { get; }

        public Order()
        {
            GoBackCommand = new Command(ExecuteGoBack);
        }

        // Метод для команды GoBackCommand
        private void ExecuteGoBack()
        {
            // Логика для возврата на предыдущую страницу
            Application.Current.MainPage.Navigation.PopAsync();
        }
        public string OrderID { get; set; } = Guid.NewGuid().ToString(); // Уникальный идентификатор


        public string DisplayOrderDate
        {
            get => OrderDate.ToString("dd.MM.yyyy"); // Форматирование даты
        }

        public DateTime CompletionDate { get; set; }
        public string FormattedCompletionDate
        {
            get => $"Дата отправки заявки: {CompletionDate.ToString("dd.MM.yyyy HH:mm")}";
        }
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (_customerName != value)
                {
                    _customerName = value;
                    OnPropertyChanged(nameof(CustomerName));
                }
            }
        }

        public List<ProductItem> Products
        {
            get => _products;
            set
            {
                if (_products != value)
                {
                    _products = value;
                    OnPropertyChanged(nameof(Products));
                }
            }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                if (_comment != value)
                {
                    _comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }

        public string Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    OnPropertyChanged(nameof(Direction));
                }
            }
        }

        public DateTime OrderDate
        {
            get => _orderDate;
            set
            {
                if (_orderDate != value)
                {
                    _orderDate = value;
                    OnPropertyChanged(nameof(OrderDate));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProductItem
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public bool IsNew { get; set; }
        public bool IsRes { get; set; }
        public bool IsEnd { get; set; }
        public decimal PricePerKg { get; set; }  // Цена за кг
        public decimal PricePerUnit { get; set; } // Цена за упаковку
        public decimal PricePerVedro { get; set; } // Цена за упаковку
        public decimal PricePerPiece { get; set; } // Цена за упаковку
        public string FormattedName
        {
            get
            {
                if (Preferences.Get("IgnoreColors", false)) // Проверка настройки
                {
                    return System.Text.RegularExpressions.Regex.Replace(Name, @"<color=#(?:[A-Fa-f0-9]{6})>(.*?)<\/color>", "$1");
                }
                return Name;
            }
        }


        public decimal PricePerCont { get; set; } // Цена за упаковку

        public string DisplayPrice
        {
            get
            {
                bool showPrice = Preferences.Get("ShowPriceEnabled", false);

                // Строка для хранения результата
                string priceString = "";

                // Проверяем и добавляем цену за килограмм, если она есть
                if (showPrice && PricePerKg > 0)
                {
                    priceString += $"{PricePerKg} руб./кг";
                }

                // Проверяем и добавляем цену за упаковку, если она есть
                if (showPrice && PricePerUnit > 0)
                {
                    if (!string.IsNullOrEmpty(priceString))
                    {
                        priceString += "; ";
                    }
                    priceString += $"{PricePerUnit} руб./уп.";
                }
                if (showPrice && PricePerVedro > 0)
                {
                    if (!string.IsNullOrEmpty(priceString))
                    {
                        priceString += "; ";
                    }
                    priceString += $"{PricePerVedro} руб./ведро";
                }
                if (showPrice && PricePerPiece > 0)
                {
                    if (!string.IsNullOrEmpty(priceString))
                    {
                        priceString += "; ";
                    }
                    priceString += $"{PricePerPiece} руб./шт.";
                }
                if (showPrice && PricePerCont > 0)
                {
                    if (!string.IsNullOrEmpty(priceString))
                    {
                        priceString += "; ";
                    }
                    priceString += $"{PricePerCont} руб./контейнер";
                }
                return priceString;
            }
        }
        public string DisplayQuantity
        {
            get
            {
                string unit = GetUnitFromName(Name);
                return Quantity % 1 == 0 ? $"{Quantity:0} {unit}" : $"{Quantity:0.##} {unit}";
            }
        }

        public string DisplayName => RemoveUnitFromName(Name);


        private string GetUnitFromName(string productName)
        {
            if (productName.EndsWith("УП.", StringComparison.OrdinalIgnoreCase))
            {
                return "уп.";
            }
            else if (productName.EndsWith("ШТ.", StringComparison.OrdinalIgnoreCase))
            {
                return "шт.";
            }
            else if (productName.EndsWith("ВЕС.", StringComparison.OrdinalIgnoreCase))
            {
                return "кг";
            }
            else if (productName.EndsWith("В.", StringComparison.OrdinalIgnoreCase))
            {
                return "в.";
            }
            else if (productName.EndsWith("КОНТ.", StringComparison.OrdinalIgnoreCase))
            {
                return "конт.";
            }
            return string.Empty; // Или можно вернуть "ед." для незнакомых наименований
        }
        private string RemoveUnitFromName(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return productName;

            string[] units = { "УП.", "ШТ.", "ВЕС.", "В.", "КОНТ." };
            foreach (var unit in units)
            {
                if (productName.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    return productName.Substring(0, productName.Length - unit.Length).Trim();
                }
            }
            return productName; // Если ни одно из условий не выполняется
        }

    }
}