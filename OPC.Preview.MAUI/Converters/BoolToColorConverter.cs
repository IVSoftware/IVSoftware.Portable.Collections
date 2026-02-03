using IVSoftware.Portable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OPC.Preview.Maui.Converters
{
    [Probationary("POC for a portable color converter that uses css colors.")]
    public class BoolToColorConverter : Portable.Converters.BoolToColorValueConverter, IValueConverter
    {
        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var html = base.Convert(value, targetType, parameter, culture);
            return Colors.Red;
        }

        public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
