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
                    string unit = GetUnitFromName(product.Name).ToLower();
                    decimal price = unit switch
                    {
                        "кг" => product.PricePerKg,
                        "уп." => product.PricePerUnit,
                        "шт." => product.PricePerPiece,
                        "конт." => product.PricePerCont,
                        "в." => product.PricePerVedro,
                        _ => 0
                    };

                    if (price > 0 && product.Quantity > 0)
                    {
                        decimal productTotal = product.Quantity * price;
                        total += productTotal;
                    }
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

        public string FormattedOrderDetails => $"{FormattedCompletionDate}\n{FormattedTotalAmount}";

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
            return string.Empty;
        }

        public string GetFormattedOrderSummaryForEmail()
        {
            var orderSummary = new List<string>();

            foreach (var product in Products)
            {
                string productName = RemoveUnitFromName(product.Name).Replace("*", "");
                string formattedQuantity = product.Quantity % 1 == 0 ? product.Quantity.ToString("0") : product.Quantity.ToString("0.#");
                string unit = GetUnitFromName(product.Name);
                string productInfo = $"{productName} - {formattedQuantity} {unit}";
                orderSummary.Add(productInfo);
            }

            return string.Join("<br>", orderSummary);
        }

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
            return productName;
        }

        public ICommand GoBackCommand { get; }

        public Order()
        {
            GoBackCommand = new Command(ExecuteGoBack);
        }

        private void ExecuteGoBack()
        {
            Application.Current.MainPage.Navigation.PopAsync();
        }

        public string OrderID { get; set; } = Guid.NewGuid().ToString();

        public string DisplayOrderDate => OrderDate.ToString("dd.MM.yyyy");

        public DateTime CompletionDate { get; set; }
        public string FormattedCompletionDate => $"Дата отправки заявки: {CompletionDate.ToString("dd.MM.yyyy HH:mm")}";

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
}