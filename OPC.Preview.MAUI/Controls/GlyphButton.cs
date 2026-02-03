using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OPC.Preview.Maui.Controls
{
    /// <summary>
    /// A button that displays a glyph instead of text.
    /// </summary>
    public class GlyphButton : ButtonBase
    {
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            Margin = new Thickness(1);
            Padding = new();
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

        public static readonly BindableProperty HoverColorProperty =
        BindableProperty.Create(
            nameof(HoverColor),
            typeof(Color),
            typeof(GlyphButton),
            defaultValue: Colors.Aqua);

        public Color HoverColor
        {
            get => (Color)GetValue(HoverColorProperty);
            set => SetValue(HoverColorProperty, value);
        }
        public bool IsPointerOver
        {
            get => _isPointerOver;
            protected set
            {
                if (!Equals(_isPointerOver, value))
                {
                    _isPointerOver = value;
                    OnIsPointerOverChanged();
                    OnPropertyChanged();
                }
            }
        }
        bool _isPointerOver = false;

        private void OnIsPointerOverChanged()
        {
            if (_isPointerOver)
            {
                _unhoveredColor = TextColor;
                TextColor = HoverColor;
            }
            else
            {
                TextColor = _unhoveredColor;
            }
        }
        Color? _unhoveredColor;

#if WINDOWS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button native)
            {
                native.IsTabStop = false;
            }
        }
#endif

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
                case nameof(OPID):
                    if (OPID is not null)
                    {
                        if (OPID.GetCustomAttribute<GlyphAttribute>()?.StdEnum is { } @enum)
                        {
                            FontFamily = @enum.ToCssFontName();
                            Text = @enum.ToGlyph();
                        }
                        else
                        {
                            LayoutOptionFlag options =
                                OPID.GetType().GetCustomAttribute<LayoutOptionsAttribute>()?.Options ?? LayoutOptionFlag.Glyph;

                            switch (options & LayoutOptionFlag.GlyphAndText)
                            {
                                case LayoutOptionFlag.Glyph:
                                    FontFamily = OPID.ToCssFontName();
                                    Text = OPID.ToGlyph();
                                    break;
                                case LayoutOptionFlag.GlyphAndText:
                                    FontFamily = OPID.ToCssFontName();
                                    Text = OPID.ToGlyph();
                                    Debug.Fail($@"ADVISORY - Assumes button is part of a composite control that will display text.");
                                    break;
                                case LayoutOptionFlag.Text:
                                    Text = OPID.ToString();
                                    break;
                            }
                        }
                    }
                    break;
                case nameof(IsVisible):
                    // Explanation:
                    // 1. Mouse over button: TextColor is highlighted.
                    // 2. Suppose that: Clicking the button makes it 'not visible'.
                    // 3. Button no longer responds to pointer exiting the rectangle.
                    // 4. So, the color would be stuck active if shown again. 
                    if (!IsVisible)
                    {
                        IsPointerOver = false;
                    }
                    break;
            }
        }
    }
}
