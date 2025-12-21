using IVSoftware.Portable.SQLiteMarkdown;
using System.Globalization;

namespace FilteredList.Maui.Demo.ValueConverters
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
            = Color.FromArgb("#106495ED");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ItemSelection state)
            {
                return state switch
                {
                    ItemSelection.Exclusive => Exclusive,
                    ItemSelection.Multi => Multi,
                    ItemSelection.Primary => Primary,
                    _ => None,
                };
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
    
    public sealed class SelectionToTextColorConverter : IValueConverter
    {
        public Color None { get; set; }
            = Application.Current?.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1E1E1E")
                : Colors.White;

        public Color Multi { get; set; } = Colors.White;

        public Color Primary { get; set; } = Colors.White;

        public Color Exclusive { get; set; } = Colors.White;

        public Color Pressed { get; set; } = Colors.White;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ItemSelection state)
            {
                return state switch
                {
                    ItemSelection.Exclusive => Exclusive,
                    ItemSelection.Multi => Multi,
                    ItemSelection.Primary => Primary,
                    _ => None,
                };
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
