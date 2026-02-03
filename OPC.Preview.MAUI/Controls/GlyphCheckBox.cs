using IVSoftware.Portable;
using System.Runtime.CompilerServices;

namespace OPC.Preview.Maui.Controls
{
    public class GlyphCheckBox : GlyphButton
    {
        public GlyphCheckBox()
        {
            OPID = GlyphProvider.IconBasics.Unchecked;
            Clicked += (sender, e) =>
            {
                if (IsLongPressDetected)
                {   /* G T K */
                    // Do not toggle on long press.
                }
                else
                {
                    IsChecked = !IsChecked;
                }
            };
            TextColor = Color.FromArgb("#80000000");
        }

        public static readonly BindableProperty IsCheckedProperty =
        BindableProperty.Create(
            nameof(IsChecked),
            typeof(bool),
            typeof(GlyphCheckBox));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            switch (propertyName)
            {
                case nameof(IsChecked):
                    OPID = 
                        IsChecked 
                        ? GlyphProvider.IconBasics.Checked
                        : GlyphProvider.IconBasics.Unchecked;
                    break;
            }
        }
        public override void OnLongPressed()
        {
            base.OnLongPressed();
        }
    }
}
