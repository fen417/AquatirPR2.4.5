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

        // Íîâûé îáðàáîò÷èê äëÿ CollectionView (âìåñòî OnOrderTapped)
        private async void OnOrderSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Order selectedOrder)
            {
                await Navigation.PushAsync(new OrderDetailsPage(selectedOrder));
                OrdersCollectionView.SelectedItem = null; // ñáðîñ âûáîðà, ÷òîáû ìîæíî áûëî ïîâòîðíî êëèêíóòü ïî ýëåìåíòó



            }
        }

        private async void OnClearOrderHistoryClicked(object sender, EventArgs e)
        {
            bool userConfirmed = await DisplayAlert("Ïîäòâåðæäåíèå",
                                                     "Âû óâåðåíû, ÷òî õîòèòå î÷èñòèòü èñòîðèþ çàêàçîâ?",
                                                     "Äà",
                                                     "Íåò");

            if (userConfirmed)
            {
                var orderHistoryService = new OrderHistoryService();
                orderHistoryService.ClearOrderHistory();
                OrdersCollectionView.ItemsSource = new List<Order>(); // î÷èñòêà âèçóàëüíî

                await DisplayAlert("Óñïåõ", "Èñòîðèÿ çàêàçîâ óñïåøíî î÷èùåíà.", "OK");
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