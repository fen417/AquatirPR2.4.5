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

        // Новый обработчик для CollectionView (вместо OnOrderTapped)
        private async void OnOrderSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Order selectedOrder)
            {
                await Navigation.PushAsync(new OrderDetailsPage(selectedOrder));
                OrdersCollectionView.SelectedItem = null; // сброс выбора, чтобы можно было повторно кликнуть по элементу
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
                OrdersCollectionView.ItemsSource = new List<Order>(); // очистка визуально

                await DisplayAlert("Успех", "История заказов успешно очищена.", "OK");
            }
        }

        public void LoadOrderHistory()
        {
            var orderHistoryService = new OrderHistoryService();
            var history = orderHistoryService.LoadOrderHistory();
            OrdersCollectionView.ItemsSource = history.Orders;
        }

        public void RefreshOrderHistory()
        {
            var orderHistoryService = new OrderHistoryService();
            var history = orderHistoryService.LoadOrderHistory();
            OrdersCollectionView.ItemsSource = history.Orders;
        }
    }
}
