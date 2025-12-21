using IVSoftware.Portable;
using System.Runtime.CompilerServices;

namespace FilteredList.Maui.Demo.Controls
{
    public class GlyphCheckBox : GlyphButton
    {
        public GlyphCheckBox()
        {
            StdIconName = GlyphProvider.IconBasics.Unchecked;
            Clicked += (sender, e) =>
            {
                IsChecked = !IsChecked;
            };
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
                    StdIconName = 
                        IsChecked 
                        ? GlyphProvider.IconBasics.Checked
                        : GlyphProvider.IconBasics.Unchecked;
                    break;
            }
        }
    }
}
