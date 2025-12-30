using FilteredList.Maui.Demo.Models;
using IVSoftware.Portable.SQLiteMarkdown;
using System.ComponentModel;
using System.Windows.Input;
namespace FilteredList.Maui.Demo.Views;

public partial class ItemCard : ContentView
{
	public ItemCard() => InitializeComponent();

    new ItemCardModel BindingContext => (ItemCardModel)base.BindingContext;


    public static readonly BindableProperty PressedCommandProperty =
        BindableProperty.Create(
            nameof(PressedCommand),
            typeof(ICommand),
            typeof(ItemCard));

    public static readonly BindableProperty ReleasedCommandProperty =
        BindableProperty.Create(
            nameof(ReleasedCommand),
            typeof(ICommand),
            typeof(ItemCard));


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