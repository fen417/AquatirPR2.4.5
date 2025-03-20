using System.Globalization;

namespace Aquatir.Model
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            string options = parameter?.ToString() ?? "True|False";
            string[] parts = options.Split('|');
            return boolValue ? parts[0] : (parts.Length > 1 ? parts[1] : string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}