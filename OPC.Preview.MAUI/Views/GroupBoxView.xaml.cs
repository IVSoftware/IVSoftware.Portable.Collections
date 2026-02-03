
using IVSoftware.Portable.Common.Exceptions;

namespace OPC.Preview.Maui.Views;

public partial class GroupBoxView : ContentView
{
	public GroupBoxView()
	{
		InitializeComponent();
        Loaded += (sender, e) => GetEffectiveBackgroundColor();
        PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(BackgroundColor):
                    GetEffectiveBackgroundColor();
                    break;
            }
        };        
	}
    private void GetEffectiveBackgroundColor()
    {
        try
        {
            if (BackgroundColor is Color color && color != Colors.Transparent)
            {
                EffectiveBackgroundColor = color;
            }
            else
            {
                var parent = Parent;
                while (parent is VisualElement el)
                {
                    if (el.BackgroundColor is not null
                        && !Equals(el.BackgroundColor, Colors.Transparent))
                    {
                        var bk = el.BackgroundColor;
                        EffectiveBackgroundColor = bk;
                        break;
                    }
                    parent = parent.Parent;
                }
            }
        }
        catch (Exception ex)
        {
            this.RethrowFramework(ex);
        }
    }

    internal static readonly BindableProperty EffectiveBackgroundColorProperty =
            BindableProperty.Create(
                propertyName: nameof(EffectiveBackgroundColor),
                returnType: typeof(Color),
                declaringType: typeof(GroupBoxView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is GroupBoxView @this)
                    {
                        // Do something with @this.EffectiveBackgroundColor
                    }
                });

    public Color EffectiveBackgroundColor
    {
        get => (Color)GetValue(EffectiveBackgroundColorProperty);
        set => SetValue(EffectiveBackgroundColorProperty, value);
    }
}