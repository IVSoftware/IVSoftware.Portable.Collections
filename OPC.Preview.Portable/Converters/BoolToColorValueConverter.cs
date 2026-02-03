using IVSoftware.Portable;
using System.Globalization;

namespace OPC.Preview.Portable.Converters
{
    [Probationary("POC for a portable color converter that uses css colors.")]
    public class BoolToColorValueConverter
    {
        public virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(parameter is Enum stdKVP)
            {

            }
            return "#00000000";
        }
        public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException("ToDo");
    }
}
