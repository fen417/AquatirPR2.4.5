using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace Aquatir
{
    public partial class OrderDetailsPage : ContentPage
    {
        public ICommand GoBackCommand { get; }

        public OrderDetailsPage(Order order)
        {
            InitializeComponent();
            BindingContext = order;
        }

        private async void OnRestoreOrderClicked(object sender, EventArgs e)
        {
            var selectedOrder = BindingContext as Order;
            if (selectedOrder != null)
            {
                await Shell.Current.GoToAsync("//MainPage");
                if (Shell.Current.CurrentPage is MainPage mainPage)
                {
                    mainPage.RestoreOrderForEditing(selectedOrder); // Восстанавливаем заказ
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось перейти к главной странице.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось восстановить заказ.", "OK");
            }
        }
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Возвращаемся на предыдущую страницу
        }
    }
}
