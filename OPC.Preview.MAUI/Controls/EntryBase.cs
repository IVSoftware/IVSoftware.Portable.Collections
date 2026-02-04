
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;


#if WINDOWS
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
#endif

namespace OPC.Preview.Maui.Controls
{
    public class EntryBase : Entry
    {
        public EntryBase()
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += OnTapped;
            GestureRecognizers.Add(tap);
        }

        /// <summary>
        /// Do this. It works. 
        /// </summary>
        /// <remarks>
        /// Don't handle the focused event which has causes 
        /// race conditions having to do with the caret.
        /// </remarks>
        public new bool Focus()
        {
            var rtn = base.Focus();
            CurrentFocused = this;
            return rtn;
        }
        EntryBase? CurrentFocused
        {
            get => _currentFocused;
            set
            {
                if (!Equals(_currentFocused, value))
                {
                    _currentFocused = value;
                    if (_currentFocused is not null && !string.IsNullOrEmpty(_currentFocused.Text))
                    {
                        _currentFocused.CursorPosition = 0;
                        _currentFocused.SelectionLength = _currentFocused.Text.Length;
                    }
                }
            }
        }
        static EntryBase? _currentFocused = default;

        private void OnTapped(object? sender, EventArgs e)
        {
            CurrentFocused = sender as EntryBase;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if WINDOWS
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox editText)
            {
                editText.KeyDown += (sender, e) =>
                {
                    switch (e.Key)
                    {
                        case VirtualKey.Tab:
                        case VirtualKey.Enter:
                            if (InputKeyboardSource
                                .GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                            {
                                OnPrevControl();
                            }
                            else
                            {
                                OnNextControl();
                            }
                            e.Handled = true;
                            break;
                    }
                };
            }
#endif

#if ANDROID
            if (Handler?.PlatformView is Android.Widget.EditText editText)
            {
                // CornflowerBlue selection highlight
                editText.SetHighlightColor(
                    Android.Graphics.Color.ParseColor("#406495ED"));
            }
#endif
        }

        protected virtual void OnNextControl()
        {
            NextControl?.Invoke(this, EventArgs.Empty);
            NextControlCommand?.Execute(this);
        }
        public event EventHandler? NextControl;
        protected virtual void OnPrevControl()
        {
            PrevControl?.Invoke(this, EventArgs.Empty);
            PrevControlCommand?.Execute(this);
        }
        public event EventHandler? PrevControl;

        public static readonly BindableProperty NextControlCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(NextControlCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(EntryBase),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is EntryBase @this)
                    {
                        // Do something with @this.NextControlCommand
                    }
                });

        public ICommand NextControlCommand
        {
            get => (ICommand)GetValue(NextControlCommandProperty);
            set => SetValue(NextControlCommandProperty, value);
        }

        public static readonly BindableProperty PrevControlCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(PrevControlCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(EntryBase),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is EntryBase @this)
                    {
                        // Do something with @this.PrevControlCommand
                    }
                });

        public ICommand PrevControlCommand
        {
            get => (ICommand)GetValue(PrevControlCommandProperty);
            set => SetValue(PrevControlCommandProperty, value);
        }
    }
}
