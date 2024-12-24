using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Aquatir.Converters
{
    public class RemoveButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверяем, является ли строка описанием суммы заказа
            string item = value as string;
            return !(item != null && item.StartsWith("Сумма заказа:"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
