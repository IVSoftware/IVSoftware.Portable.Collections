using IVSoftware.Portable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if WINDOWS
using Microsoft.UI.Xaml.Input;
#endif

namespace FilteredList.Maui.Demo.Controls
{
    /// <summary>
    /// A button that displays a glyph instead of text.
    /// </summary>
    public class GlyphButton : Button
    {
        public GlyphButton()
        {
            VerticalOptions = LayoutOptions.Fill;
            Margin = new Thickness(1);
            Padding = new();
            FontSize = 18;
            BorderWidth = 0;
        }
        public static readonly BindableProperty StdIconNameProperty =
            BindableProperty.Create(
                nameof(StdIconName),
                typeof(Enum),
                typeof(GlyphButton));

        public Enum? StdIconName
        {
            get => (Enum?)GetValue(StdIconNameProperty);
            set => SetValue(StdIconNameProperty, value);
        }

        public static readonly BindableProperty WidthTracksHeightProperty =
        BindableProperty.Create(
            nameof(WidthTracksHeight),
            typeof(bool),
            typeof(GlyphButton),
            defaultValue: true);

        public bool WidthTracksHeight
        {
            get => (bool)GetValue(WidthTracksHeightProperty);
            set => SetValue(WidthTracksHeightProperty, value);
        }

        public static readonly BindableProperty ActiveColorProperty =
        BindableProperty.Create(
            nameof(ActiveColor),
            typeof(Color),
            typeof(GlyphButton),
            defaultValue: Colors.Aqua);

        public Color ActiveColor
        {
            get => (Color)GetValue(ActiveColorProperty);
            set => SetValue(ActiveColorProperty, value);
        }
        /// <summary>
        /// Revert value that will always be set before actual use.
        /// </summary>
        private Color _inactiveColor = new();

        protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            switch (propertyName)
            {
                case nameof(Height):
                    if (WidthTracksHeight)
                    {
                        WidthRequest = Height;
                    }
                    break;
                case nameof(StdIconName):
                    if(StdIconName is not null)
                    {
                        FontFamily = StdIconName.ToCssFontName();
                        Text = StdIconName.ToGlyph();
                    }
                    break;
            }
        }

#if WINDOWS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button native)
            {
                native.PointerEntered -= OnPointerEntered;
                native.PointerEntered += OnPointerEntered;
                native.PointerExited -= OnPointerExited;
                native.PointerExited += OnPointerExited;
            }
        }
        void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _inactiveColor = TextColor;
            TextColor = ActiveColor;
        }
        void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            TextColor = _inactiveColor;
        }
#endif
    }
}
