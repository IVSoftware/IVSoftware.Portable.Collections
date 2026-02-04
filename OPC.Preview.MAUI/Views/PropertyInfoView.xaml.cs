using OPC.Preview.Portable.Models;

namespace OPC.Preview.Maui.Views;

public partial class PropertyInfoView : ContentView
{
    public PropertyInfoView()
    {
        InitializeComponent();
        BindingContextChanged += (sender, e) =>
        {
		    if (BindingContext is PropertyInfoModel model)
		    {
                model.FocusEntry = FocusEntry;
            }
        };
    }
    protected void FocusEntry()
    {
        Dispatcher.Dispatch(() =>
        {
            if (!Entry.IsFocused)
            {
                Entry.Focus();
            };
        });
    }
}