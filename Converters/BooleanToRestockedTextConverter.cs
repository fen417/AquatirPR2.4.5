using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Aquatir.Converters
{
    public class BooleanToRestockedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isRes && isRes ? " Снова в продаже! " : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;  // Конвертация в обратную сторону не требуется
        }
    }
}
