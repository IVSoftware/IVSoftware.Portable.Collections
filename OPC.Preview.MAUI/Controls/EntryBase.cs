using System;
using System.Collections.Generic;
using System.Text;

namespace OPC.Preview.Maui.Controls
{
    public class EntryBase : Entry
    {
        public EntryBase()
        {
            Focused += OnFocused;

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnTapped;
            GestureRecognizers.Add(tap);
        }

        private void OnFocused(object? sender, FocusEventArgs e)
            => SelectAllDeferred();

        private void OnTapped(object? sender, EventArgs e)
            => SelectAllDeferred();

        private void SelectAllDeferred()
        {
            Dispatcher.Dispatch(() =>
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    CursorPosition = 0;
                    SelectionLength = Text.Length;
                }
            });
        }
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler?.PlatformView is null)
                return;

#if ANDROID
            if (Handler.PlatformView is Android.Widget.EditText editText)
            {
                // CornflowerBlue selection highlight
                editText.SetHighlightColor(
                    Android.Graphics.Color.ParseColor("#406495ED"));
            }
#endif
        }
    }
}
