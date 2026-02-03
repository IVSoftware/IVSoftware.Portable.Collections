using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace OPC.Preview.Maui.Controls
{
    public class QueryFilterEntry : EntryBase
    {
        public QueryFilterEntry() { }

#if WINDOWS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                tb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Transparent);

                tb.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Transparent);

                // Focused
                tb.Resources["TextControlBorderThicknessFocused"] =
                    new Microsoft.UI.Xaml.Thickness(0);
                tb.Resources["TextControlBorderBrushFocused"] =
                    new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Transparent);

                // Unfocused
                tb.Resources["TextControlBorderThicknessUnfocused"] =
                    new Microsoft.UI.Xaml.Thickness(0);
                tb.Resources["TextControlBorderBrushUnfocused"] =
                    new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Transparent);

                // PointerOver (hover)
                tb.Resources["TextControlBorderThicknessPointerOver"] =
                    new Microsoft.UI.Xaml.Thickness(0);
                tb.Resources["TextControlBorderBrushPointerOver"] =
                    new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Transparent);
            }
        }
#elif ANDROID
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler?.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText et)
            {
                // Remove the default Material background.
                et.Background = null;

                // Ensure no residual tint or underline sneaks back in.
                et.BackgroundTintList = null;

                // Make the control visually transparent.
                et.SetBackgroundColor(Android.Graphics.Color.Transparent);

                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    et.TextCursorDrawable = null; // forces default system caret
#if false && SAVE
                    var resId = Android.Resource.Drawable.EditText;
                    et.SetTextCursorDrawable(resId);
#endif
                }
            }
        }
#elif IOS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
        }
#endif
    }
}
