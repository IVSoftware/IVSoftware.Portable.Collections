using System.Runtime.CompilerServices;

namespace FilteredList.Maui.Demo.Controls
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
                    BorderWidth = ToggleState ? 1.5 : 0;
                    break;
            }
        }
    }
}
