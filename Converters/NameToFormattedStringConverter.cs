using System.Globalization;
using System.Text.RegularExpressions;

namespace Aquatir.Converters
{
    public class NameToFormattedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name)
            {
                bool ignoreColors = Preferences.Get("IgnoreColors", false);

                if (ignoreColors)
                {
                    return Regex.Replace(name, @"<color=#(?:[A-Fa-f0-9]{6})>(.*?)<\/color>", "$1");
                }

                var formattedString = new FormattedString();
                var regex = new Regex(@"<color=#(?<color>[A-Fa-f0-9]{6})>(?<text>.*?)<\/color>");
                var matches = regex.Matches(name);
                int lastIndex = 0;

                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        formattedString.Spans.Add(new Span
                        {
                            Text = name.Substring(lastIndex, match.Index - lastIndex)
                        });
                    }

                    formattedString.Spans.Add(new Span
                    {
                        Text = match.Groups["text"].Value,
                        TextColor = Microsoft.Maui.Graphics.Color.FromArgb($"#{match.Groups["color"].Value}")
                    });

                    lastIndex = match.Index + match.Length;
                }

                if (lastIndex < name.Length)
                {
                    formattedString.Spans.Add(new Span
                    {
                        Text = name.Substring(lastIndex)
                    });
                }

                return formattedString;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
