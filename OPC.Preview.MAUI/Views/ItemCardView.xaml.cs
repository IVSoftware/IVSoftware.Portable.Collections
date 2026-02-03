using OPC.Preview.Portable.Models;
using System.Windows.Input;
namespace OPC.Preview.Maui.Views;

public partial class ItemCardView : ContentView
{
	public ItemCardView() => InitializeComponent();

    new ItemCardModel BindingContext => (ItemCardModel)base.BindingContext;


    public static readonly BindableProperty PressedCommandProperty =
        BindableProperty.Create(
            nameof(PressedCommand),
            typeof(ICommand),
            typeof(ItemCardView));

    public static readonly BindableProperty ReleasedCommandProperty =
        BindableProperty.Create(
            nameof(ReleasedCommand),
            typeof(ICommand),
            typeof(ItemCardView));


    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        PressedCommand?.Execute(BindingContext);
        BindingContext.IsPressed = true;
    }
    public ICommand? PressedCommand
    {
        get => (ICommand?)GetValue(PressedCommandProperty);
        set => SetValue(PressedCommandProperty, value);
    }

    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
        ReleasedCommand?.Execute(BindingContext);
        BindingContext.IsPressed = false;
    }

    public ICommand? ReleasedCommand
    {
        get => (ICommand?)GetValue(ReleasedCommandProperty);
        set => SetValue(ReleasedCommandProperty, value);
    }
}