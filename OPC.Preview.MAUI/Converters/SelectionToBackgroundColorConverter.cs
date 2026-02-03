using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using OPC.Preview.Maui.Models;
using System.Globalization;

namespace OPC.Preview.Maui.Converters
{
    public sealed class SelectionToBackgroundColorConverter : IValueConverter
    {
        public Color None { get; set; }
            = Application.Current?.RequestedTheme == AppTheme.Light
                ? Colors.White 
                : Color.FromArgb("#1E1E1E");

        public Color Multi { get; set; }
            = Color.FromArgb("#7D6495ED");

        public Color Primary { get; set; }
            = Color.FromArgb("#C86495ED");

        public Color Exclusive { get; set; }
            = Color.FromArgb("#FF6495ED");

        public Color Pressed { get; set; }
            = Color.FromArgb("#206495ED");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ItemSelection state)
            {
                if (state.HasFlag((ItemSelection)TrackStateEphemeral.Pressed))
                {
                    return Pressed;
                }
                else
                {
                    return state switch
                    {
                        ItemSelection.Exclusive => Exclusive,
                        ItemSelection.Multi => Multi,
                        ItemSelection.Primary => Primary,
                        _ => None,
                    };
                }
            }
            else
            {
                return None;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
