using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace Aquatir
{
    public partial class OrderHistoryPage : ContentPage
    {
        public OrderHistoryPage()
        {
            InitializeComponent();
            LoadOrderHistory();
        }
        private void OnRefreshButtonClicked(object sender, EventArgs e)
        {
            RefreshOrderHistory();
        }
        private async void OnOrderTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is Order selectedOrder && selectedOrder != null)
            {
                await Navigation.PushAsync(new OrderDetailsPage(selectedOrder));
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось открыть заказ. Пожалуйста, попробуйте снова.", "OK");
            }
        }
        private async void OnClearOrderHistoryClicked(object sender, EventArgs e)
        {
            bool userConfirmed = await DisplayAlert("Подтверждение",
                                                     "Вы уверены, что хотите очистить историю заказов?",
                                                     "Да",
                                                     "Нет");

            if (userConfirmed)
            {
                var orderHistoryService = new OrderHistoryService();
                orderHistoryService.ClearOrderHistory();

                // Обновляем список заказов
                OrdersListView.ItemsSource = new List<Order>();

                await DisplayAlert("Успех", "История заказов успешно очищена.", "OK");
            }
        }
        public void LoadOrderHistory()
        {
            var orderHistoryService = new OrderHistoryService();
            var history = orderHistoryService.LoadOrderHistory();
            OrdersListView.ItemsSource = history.Orders;
        }
        public void RefreshOrderHistory()
        {
            // Загружаем обновленную историю заказов
            var orderHistoryService = new OrderHistoryService();
            var history = orderHistoryService.LoadOrderHistory();
            OrdersListView.ItemsSource = history.Orders;
        }
    }
}
