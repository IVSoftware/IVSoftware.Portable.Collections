using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace OPC.Preview.Maui.Views
{
    public class OverlayView : ContentView 
    {
        public OverlayView()
        {
            var gr = new TapGestureRecognizer();
            gr.Tapped += (sender, e) => OnTapped(e);
            GestureRecognizers.Add(gr);
        }

        public virtual void OnTapped(TappedEventArgs e)
        {
            Tapped?.Invoke(this, e);
            TapOverlayCommand?.Execute(BindingContext);
        }

        public event EventHandler<TappedEventArgs>? Tapped;

        public static readonly BindableProperty TapOverlayCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(TapOverlayCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(OverlayView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is OverlayView @this)
                    {
                        // Do something with @this.OverlayTappecCommand
                    }
                });

        public ICommand TapOverlayCommand
        {
            get => (ICommand)GetValue(TapOverlayCommandProperty);
            set => SetValue(TapOverlayCommandProperty, value);
        }
    }
}
