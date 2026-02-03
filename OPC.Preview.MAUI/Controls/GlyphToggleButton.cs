using System.Runtime.CompilerServices;

namespace OPC.Preview.Maui.Controls
{
    public class GlyphToggleButton : GlyphButton
    {
        public GlyphToggleButton() 
        {
            Clicked += (sender, e) =>
            {
                ToggleState = !ToggleState;
            };
            BorderColor = Colors.DarkSalmon;
        }
        public static readonly BindableProperty ToggleStateProperty =
        BindableProperty.Create(
            nameof(ToggleState),
            typeof(bool),
            typeof(GlyphToggleButton));

        public bool ToggleState
        {
            get => (bool)GetValue(ToggleStateProperty);
            set => SetValue(ToggleStateProperty, value);
        }
        protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            switch (propertyName)
            {
                case nameof(ToggleState):
                    BorderWidth = ToggleState ? BorderWidthToggled : 0;
                    break;
            }
        }

        public static readonly BindableProperty BorderWidthToggledProperty =
            BindableProperty.Create(
                propertyName: nameof(GlyphToggleButton.BorderWidthToggled),
                returnType: typeof(double),
                declaringType: typeof(GlyphToggleButton),
                defaultValue: 1.5,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is GlyphToggleButton @this)
                    {
                        if(@this.ToggleState)
                        {
                            @this.BorderWidth = @this.BorderWidthToggled;
                        }
                    }
                });

        public double BorderWidthToggled
        {
            get => (double)GetValue(BorderWidthToggledProperty);
            set => SetValue(BorderWidthToggledProperty, value);
        }
        public override void OnLongPressed()
        {
            base.OnLongPressed();
        }
    }
}
