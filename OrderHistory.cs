using System.Collections.Generic;
using System.Linq;

namespace Aquatir
{
    public class OrderHistory
    {
        public List<Order> Orders { get; set; } = new List<Order>();

        public void AddOrder(Order order)
        {
            if (order != null)
            {
                Orders.Add(order);
            }
        }
        public void UpdateOrder(Order order)
        {
            try
            {
                var existingOrder = Orders.FirstOrDefault(o => o.OrderID == order.OrderID);
                if (existingOrder != null)
                {
                    existingOrder.Direction = order.Direction;
                    existingOrder.Comment = order.Comment;
                    existingOrder.Products = order.Products; // Обновляем список продуктов
                    existingOrder.CompletionDate = order.CompletionDate; // Обновляем дату завершения
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления заказа: {ex.Message}");
            }
        }

    }
}
