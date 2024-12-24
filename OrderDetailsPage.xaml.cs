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
            BindingContext = order; // ������������� �������� �������� �� ��������� �����
        }

        private async void OnRestoreOrderClicked(object sender, EventArgs e)
        {
            var selectedOrder = BindingContext as Order;
            if (selectedOrder != null)
            {
                // ������� �� ������� ��������
                await Shell.Current.GoToAsync("//MainPage");

                // ��������, ��� ������� �������� ������������� MainPage
                if (Shell.Current.CurrentPage is MainPage mainPage)
                {
                    mainPage.RestoreOrderForEditing(selectedOrder); // ��������������� �����
                }
                else
                {
                    await DisplayAlert("������", "�� ������� ������� � ������� ��������.", "OK");
                }
            }
            else
            {
                await DisplayAlert("������", "�� ������� ������������ �����.", "OK");
            }
        }
    }
}
