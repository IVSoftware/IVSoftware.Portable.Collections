using OPC.Preview.Portable.Models;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OPC.Preview.Maui.Views;

public partial class SelectableQFView : ContentView
{
    public SelectableQFView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Workaround
    /// Compensates for spurious layout errors that
    /// are apparently due to conditional visibility.
    /// </summary>
    void RefreshCheckboxStyles()
    {
        Dispatcher.Dispatch(() =>
        {
            if (Application.Current?.Resources.TryGetValue("GlyphCheckBox", out var unk) == true
                && unk is Style style
                && CheckBox.IsVisible)
            {
                foreach (var setter in style.Setters)
                {
                    if (setter is Setter s)
                    {
                        CheckBox.SetValue(s.Property, s.Value);
                    }
                }
            }
        });
    }

    new ItemCardModel BindingContext => (ItemCardModel)base.BindingContext;


    public static readonly BindableProperty PointerPressedCommandProperty =
        BindableProperty.Create(
            nameof(PointerPressedCommand),
            typeof(ICommand),
            typeof(SelectableQFView));

    /// <summary>
    /// From XAML - Redirect with Item
    /// </summary>
    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        BindingContext.IsPressed = true;
        PointerPressedCommand?.Execute(BindingContext);
    }
    public ICommand? PointerPressedCommand
    {
        get => (ICommand?)GetValue(PointerPressedCommandProperty);
        set => SetValue(PointerPressedCommandProperty, value);
    }

    public static readonly BindableProperty PointerReleasedCommandProperty =
        BindableProperty.Create(
            nameof(PointerReleasedCommand),
            typeof(ICommand),
            typeof(SelectableQFView));

    /// <summary>
    /// From XAML - Redirect with Item
    /// </summary>
    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
        BindingContext.IsPressed = false;
        PointerReleasedCommand?.Execute(BindingContext);
    }

    public ICommand? PointerReleasedCommand
    {
        get => (ICommand?)GetValue(PointerReleasedCommandProperty);
        set => SetValue(PointerReleasedCommandProperty, value);
    }

    /// <summary>
    /// From XAML - Redirect with PointerEventArgs
    /// </summary>
    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        PointerMovedCommand?.Execute(e);
    }

    public static readonly BindableProperty PointerMovedCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(PointerMovedCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay);

    public ICommand? PointerMovedCommand
    {
        get => (ICommand?)GetValue(PointerMovedCommandProperty);
        set => SetValue(PointerMovedCommandProperty, value);
    }


    public static readonly BindableProperty PointerExitedCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(PointerExitedCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay);

    public ICommand PointerExitedCommand
    {
        get => (ICommand)GetValue(PointerExitedCommandProperty);
        set => SetValue(PointerExitedCommandProperty, value);
    }

    /// <summary>
    /// From XAML - Redirect with PointerEventArgs
    /// </summary>
    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        PointerExitedCommand?.Execute(e);
    }


    public static readonly BindableProperty CheckBoxPressedCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(CheckBoxPressedCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is SelectableQFView @this)
                    {
                        // Do something with @this.CheckBoxPressedCommand
                    }
                });

    public ICommand CheckBoxPressedCommand
    {
        get => (ICommand)GetValue(CheckBoxPressedCommandProperty);
        set => SetValue(CheckBoxPressedCommandProperty, value);
    }

    public static readonly BindableProperty CheckBoxLongPressedCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(CheckBoxLongPressedCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay);

    public ICommand CheckBoxLongPressedCommand
    {
        get => (ICommand)GetValue(CheckBoxLongPressedCommandProperty);
        set => SetValue(CheckBoxLongPressedCommandProperty, value);
    }


    public static readonly BindableProperty CheckBoxReleasedCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(CheckBoxReleasedCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is SelectableQFView @this)
                    {
                        // Do something with @this.CheckBoxReleased
                    }
                });

    public ICommand CheckBoxReleasedCommand
    {
        get => (ICommand)GetValue(CheckBoxReleasedCommandProperty);
        set => SetValue(CheckBoxReleasedCommandProperty, value);
    }

    public static readonly BindableProperty IsCheckBoxVisibleProperty =
            BindableProperty.Create(
                propertyName: nameof(IsCheckBoxVisible),
                returnType: typeof(bool),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is SelectableQFView @this 
                    && newValue is bool isCheckBoxVisible)
                    {
                        @this.Grid.ColumnDefinitions[0].Width =
                        isCheckBoxVisible
                            ? new GridLength(40, GridUnitType.Absolute)
                            : new GridLength(0, GridUnitType.Absolute);
                    }
                });

    public bool IsCheckBoxVisible
    {
        get => (bool)GetValue(IsCheckBoxVisibleProperty);
        set{SetValue(IsCheckBoxVisibleProperty, value);}
    }

    public static readonly BindableProperty CheckBoxClickableEventCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(CheckBoxClickableEventCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(SelectableQFView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is SelectableQFView @this)
                    {
                        // Do something with @this.CheckBoxClickableEventCommand
                    }
                });

    public ICommand CheckBoxClickableEventCommand
    {
        get => (ICommand)GetValue(CheckBoxClickableEventCommandProperty);
        set => SetValue(CheckBoxClickableEventCommandProperty, value);
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        switch (propertyName)
        {
            case nameof(IsCheckBoxVisible):
                RefreshCheckboxStyles();
                break;
        }
    }
}