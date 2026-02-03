using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using OPC.Preview.Maui.Models;
using System.Globalization;

namespace OPC.Preview.Maui.Converters
{

    public sealed class SelectionToTextColorConverter : IValueConverter
    {
        public Color None { get; set; }
            = Application.Current?.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1E1E1E")
                : Colors.White;

        public Color Multi { get; set; } = Colors.White;

        public Color Primary { get; set; } = Colors.White;

        public Color Exclusive { get; set; } = Colors.White;

        public Color Pressed { get; set; } = Colors.Gray;

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
                    switch (state)
                    {
                        default:
                        case ItemSelection.None:
                            if (parameter.TryResolveColorParameter(out var light, out var dark))
                            {
                                return
                                    Application.Current?.RequestedTheme == AppTheme.Dark
                                    ? dark
                                    : light;
                            }
                            else return None;
                        case ItemSelection.Exclusive: return Exclusive;
                                case ItemSelection.Multi: return Multi;
                                case ItemSelection.Primary: return Primary;
                                }
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
    public static class ConverterExtensions
    {
        public static bool TryResolveColorParameter(
            this object? parameters,
            out Color light,
            out Color dark)
        {
            light = Colors.Transparent;
            dark = Colors.Transparent;
            if(parameters is not string colors || string.IsNullOrWhiteSpace(colors))
                return false;

            var theme = colors
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (theme.Length == 0 || theme.Length > 2)
                return false;

            try
            {
                light = Color.Parse(theme[0]);
                dark = theme.Length == 2 ? Color.Parse(theme[1]) : light;
                return true;
            }
            catch
            {
                light = Colors.Transparent;
                dark = Colors.Transparent;
                return false;
            }
        }
    }
}
