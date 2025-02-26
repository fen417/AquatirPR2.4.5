using System.IO;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;

namespace Aquatir
{
    public class OrderHistoryService
    {
        private string GetOrderHistoryFilePath()
        {
            string folderPath = FileSystem.AppDataDirectory;
            return Path.Combine(folderPath, "orderHistory.json");
        }

        public void SaveOrderHistory(OrderHistory orderHistory)
        {
            var filePath = GetOrderHistoryFilePath();
            var json = JsonConvert.SerializeObject(orderHistory);
            File.WriteAllText(filePath, json);
        }

        public OrderHistory LoadOrderHistory()
        {
            var filePath = GetOrderHistoryFilePath();
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var orderHistory = JsonConvert.DeserializeObject<OrderHistory>(json) ?? new OrderHistory();
                foreach (var order in orderHistory.Orders)
                {
                    foreach (var product in order.Products)
                    {
                        product.Name = RemoveColorTags(product.Name);
                    }
                }

                return orderHistory;
            }
            return new OrderHistory();
        }
        private string RemoveColorTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return Regex.Replace(input, @"<color=[^>]+>|</color>", string.Empty);
        }
        public void ClearOrderHistory()
        {
            var filePath = GetOrderHistoryFilePath();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}