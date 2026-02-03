using System.Globalization;

namespace OPC.Preview.Maui.Converters
{
    public class BoolToGridLengthConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                parameter ??= "*";
                if (isVisible)
                {
                    switch (parameter)
                    {
                        case string s when s.Equals("Auto", StringComparison.OrdinalIgnoreCase):
                            return GridLength.Auto;
                        default:
                            if(double.TryParse(
                                parameter.ToString(),
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var width
                            ))
                            {
                                return new GridLength(width);
                            }
                            else
                            {
                                return GridLength.Star;
                            }
                    }
                }
                else
                {
                    return new GridLength(0);
                }
            }
            else
            {
                return GridLength.Star;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
