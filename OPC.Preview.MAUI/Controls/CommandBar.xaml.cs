using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using OPC.Preview.Portable;
using OPC.Preview.Portable.Events;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OPC.Preview.Maui.Controls;

public partial class CommandBar
    : ContentView
    , IOPCommandBar
{
    public CommandBar()
    {
        Content = Grid;
        IsVisible = VisibleItems.Any();
        NestedClickableEventCommand = new Command<ClickableEventArgs>(OnNestedClickableEvent);
    }
    internal ICommand NestedClickableEventCommand { get; }
    private void OnNestedClickableEvent(ClickableEventArgs e)
    {
        e = new ClickableEventArgs(this, e);
        ClickableEventCommand?.Execute(e);
    }
    public Grid Grid
    {
        get
        {
            if (_grid is null)
            {
                _grid = new();
                _grid.ChildAdded += (sender, e) =>
                {
                    if (e.Element is GlyphButton item)
                    {
                        item.Margin = ChildMargin;
                        item.PropertyChanged += OnChildPropertyChanged;
                        item.ClickableEventCommand = NestedClickableEventCommand;
                    }
                    _itemsVisibleDirty = _itemsDirty = true;
                };
                _grid.ChildRemoved += (sender, e) =>
                {
                    if (e.Element is GlyphButton item)
                    {
                        item.PropertyChanged -= OnChildPropertyChanged;
                    }
                    _itemsVisibleDirty = _itemsDirty = true;
                };
            }
            return _grid;
        }
    }
    Grid? _grid = null;

    public GlyphButton[] Items
    {
        get
        {
            if (_itemsDirty)
            {
                _itemsDirty = false;
                _items = Grid
                .Children
                .OfType<GlyphButton>()
                .ToArray();
            }
            return _items;
        }
    }
    GlyphButton[] _items = [];
    bool _itemsDirty = false;

    public GlyphButton[] VisibleItems
    {
        get
        {
            if(_itemsVisibleDirty)
            {
                _itemsVisibleDirty = false;
                _visibleItems = Grid
                .Children
                .OfType<GlyphButton>()
                .Where(_ => _.IsVisible)
                .ToArray();
            }
            return _visibleItems;
        }
    }
    GlyphButton[] _visibleItems = [];
    bool _itemsVisibleDirty = false;

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is GlyphButton item)
            switch (e.PropertyName)
            {
                case nameof(IsVisible):
                    if (LayoutOptions.HasFlag(LayoutOptionFlag.Vertical))
                    {
                        Grid
                            .RowDefinitions[Grid.GetRow((IView)item)].Height =
                                item.IsVisible
                                ? RowHeightRequest
                                : 0;
                    }
                    else
                    {
                        Grid
                            .ColumnDefinitions[Grid.GetColumn((IView)item)].Width =
                                item.IsVisible
                                ? GridLength.Star
                                : 0;
                    }
                    _itemsVisibleDirty = true;
                    OnPropertyChanged(nameof(VisibleItems));
                    break;
                case nameof(VisibleItems):
                    if(VisibleItems.Length == 0)
                    { }
                    break;
            }
    }

    public void Configure<T>() => Configuration = typeof(T);
    internal int ColumnCount
    {
        get => Grid.ColumnDefinitions.Count;
        set
        {
            switch (Grid.ColumnDefinitions.Count.CompareTo(value))
            {
                case -1:
                    while (Grid.ColumnDefinitions.Count < value)
                    {
                        Grid.ColumnDefinitions.Add(
                            new ColumnDefinition
                            {
                                Width = GridLength.Star
                            });
                    }
                    break;
                default:
                    return;
                case 1:
                    while (Grid.ColumnDefinitions.Count > value)
                    {
                        Grid.ColumnDefinitions.RemoveAt(value);
                    }
                    break;
            }
        }
    }
    internal int RowCount
    {
        get => Grid.RowDefinitions.Count;
        set
        {
            switch (Grid.RowDefinitions.Count.CompareTo(value))
            {
                case -1:
                    while (Grid.RowDefinitions.Count < value)
                    {
                        Grid.RowDefinitions.Add(
                            new RowDefinition
                            {
                                Height = RowHeightRequest
                            });
                    }
                    break;
                default:
                    return;
                case 1:
                    while (Grid.RowDefinitions.Count > value)
                    {
                        Grid.RowDefinitions.RemoveAt(value);
                    }
                    break;
            }
        }
    }

    public event EventHandler<ItemClickedEventArgs>? ChildClicked;


    public static readonly BindableProperty ChildMarginProperty =
            BindableProperty.Create(
                propertyName: nameof(ChildMargin),
                returnType: typeof(Thickness),
                declaringType: typeof(CommandBar),
                defaultValue: new Thickness(5),
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is CommandBar @this)
                    {
                        // Do something with @this.ChildMargin
                    }
                });

    public Thickness ChildMargin
    {
        get => (Thickness)GetValue(ChildMarginProperty);
        set => SetValue(ChildMarginProperty, value);
    }


    public static readonly BindableProperty ChildStyleProperty =
            BindableProperty.Create(
                propertyName: nameof(ChildStyle),
                returnType: typeof(Style),
                declaringType: typeof(CommandBar),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is CommandBar @this)
                    {
                        if (newValue is Style style)
                        {
                            foreach (var item in @this.Items)
                            {
                                item.Style = style;
                            }
                        }
                        else
                        {
                            foreach (var item in @this.Items)
                            {
                                item.Style = null;
                            }
                        }
                    }
                });

    public Style ChildStyle
    {
        get => (Style)GetValue(ChildStyleProperty);
        set => SetValue(ChildStyleProperty, value);
    }


    public static readonly BindableProperty RowHeightRequestProperty =
            BindableProperty.Create(
                propertyName: nameof(RowHeightRequest),
                returnType: typeof(double),
                declaringType: typeof(CommandBar),
                defaultValue: 40.0,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is CommandBar @this)
                    {
                        // Do something with @this.RowHeightRequest
                    }
                });

    public double RowHeightRequest
    {
        get => (double)GetValue(RowHeightRequestProperty);
        set => SetValue(RowHeightRequestProperty, value);
    }

    public static readonly BindableProperty TrackContextProperty =
            BindableProperty.Create(
                propertyName: nameof(TrackContext),
                returnType: typeof(ITrackContext),
                declaringType: typeof(CommandBar),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is CommandBar @this)
                    {
                        (oldValue as ITrackContext)?.PropertyChanged -= localOnTrackContextChanged;
                        (newValue as ITrackContext)?.PropertyChanged += localOnTrackContextChanged;

                        // Rasise first, then subscribe to changes to the
                        // properties of the tracking context in order to
                        // trigger child visibility updates for command bar.
                        @this.UpdateChildVisibility();
                        void localOnTrackContextChanged(object? sender, PropertyChangedEventArgs e)
                        {
                            if (@this.Configuration is { } config)
                            {
                                switch (e.PropertyName)
                                {
                                    case nameof(ITrackContext.CurrentItems):
                                        @this.UpdateChildVisibility();
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                });

    public ITrackContext? TrackContext
    {
        get => (ITrackContext?)GetValue(TrackContextProperty);
        set => SetValue(TrackContextProperty, value);
    }

    /// <summary>
    /// Tracks visibility with respect to [VisibilityPredicate].
    /// </summary>
    /// <remarks>
    /// This extra sophistication makes it a poor candidate for
    /// the IsVisibleToColumnWidthConverter which is too naive.
    /// </remarks>
    private void UpdateChildVisibility()
    {
        var trackCount = TrackContext?.Count;
        if (Configuration is { } config && config.IsEnum)
        {
            var gbs = Grid.Children.OfType<GlyphButton>().ToArray();
            // [Careful]
            // Don't use gb.OPID which is more likely to be a
            // mapped icon than a member with a [VisibilityPredicate]. 
            var members = Enum.GetValues(config).Cast<Enum>().ToArray();
            Enum member;
            GlyphButton gb;

            if (gbs.Length != members.Length)
            {
                this.ThrowFramework<InvalidOperationException>($"Expecting a 1:1 relationship of config enum members to children.");
            }
            else
            {
                for (int i = 0; i < members.Length; i++)
                {
                    member = members[i];
                    gb = gbs[i];

                    var visibility =
                        member
                        .GetCustomAttribute<VisibilityPredicateAttribute>()
                        ?.Visibility
                        ?? VisibilityPredicateFlag.Always;
                    if (visibility == VisibilityPredicateFlag.Always)
                    {
                        gb.IsVisible = true;
                    }
                    else
                    {
                        switch (trackCount)
                        {
                            case null:
                                gb.IsVisible = true;
                                break;
                            case 0:
                                gb.IsVisible = false;
                                break;
                            case 1:
                                gb.IsVisible = visibility.HasFlag(VisibilityPredicateFlag.Single);
                                break;
                            default:
                                gb.IsVisible = visibility.HasFlag(VisibilityPredicateFlag.Multiple);
                                break;
                        }
                    }
                }
                IsVisible = VisibleItems.Any();
            }
        }
    }

    /// <summary>
    /// Bindable single configuration.
    /// </summary>
    public static readonly BindableProperty ConfigurationProperty =
            BindableProperty.Create(
                propertyName: nameof(Configuration),
                returnType: typeof(Type),
                declaringType: typeof(CommandBar),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is CommandBar @this && newValue is Type type)
                    {
                        @this.OnConfigurationChanged();
                    }
                });

    private void OnConfigurationChanged()
    {
        if (Configuration.IsEnum)
        {
            LayoutOptions = 
                Configuration.GetCustomAttribute<LayoutOptionsAttribute>()?.Options
                ?? LayoutOptionFlag.Horizontal | LayoutOptionFlag.Glyph;
            Grid.Children.Clear();
            Enum[] values = Enum.GetValues(Configuration).Cast<Enum>().ToArray();

            if (LayoutOptions.HasFlag(LayoutOptionFlag.Vertical))
            {
                ColumnCount = 1;
                RowCount = values.Length;
                for (int i = 0; i < RowCount; i++)
                {
                    var button = new GlyphButton 
                    { 
                        OPID = values[i],
                    };
                    if(LayoutOptions.HasFlag(LayoutOptionFlag.Text))
                    {
                        button.Text = values[i].ToString();
                    }
                    if (ChildStyle is not null)
                    {
                        button.Style = ChildStyle;
                    }
                    Grid.Add(button, row: i);
                }
            }
            else
            {
                RowCount = 1;
                ColumnCount = values.Length;
                for (int i = 0; i < ColumnCount; i++)
                {
                    var button = new GlyphButton { OPID = values[i] };
                    if (ChildStyle is not null)
                    {
                        button.Style = ChildStyle;
                    }
                    Grid.Add(button, column: i);
                }
            }
            UpdateChildVisibility();
        }
        else
        {
            this.ThrowHard<NotSupportedException>(
                $"{nameof(CommandBar)} configuration requires {nameof(Enum)} type.");
        }
    }

    public Type Configuration
    {
        get => (Type)GetValue(ConfigurationProperty);
        set
        {
            if (value.IsEnum)
            {
                SetValue(ConfigurationProperty, value);
            }
            else
            {
                this.ThrowHard<InvalidOperationException>($"Expecting Enum Type but received {value.Name}");
            }
        }
    }

    protected override void OnPropertyChanged([CallerMemberName]string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        switch (propertyName)
        {
            case nameof(Height):
                break;
            case nameof(VisibleItems):
                break;
        }
    }
    public void Configure(Type? type)
    {
        if(type is null)
        {
            this.ThrowHard<ArgumentNullException>(
                $"{nameof(CommandBar)} requires non-null configuration.");
        }
        else
        {
            Configuration = type;
        }
    }
    public LayoutOptionFlag LayoutOptions
    {
        get => _layoutOptions;
        set
        {
            if (!Equals(_layoutOptions, value))
            {
                _layoutOptions = value;
                OnPropertyChanged();
            }
        }
    }
    LayoutOptionFlag _layoutOptions = default;


    public static readonly BindableProperty ClickableEventCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(ClickableEventCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(CommandBar),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is CommandBar @this)
                    {
                        // Do something with @this.ClickableEventCommand
                    }
                });

    public ICommand ClickableEventCommand
    {
        get => (ICommand)GetValue(ClickableEventCommandProperty);
        set => SetValue(ClickableEventCommandProperty, value);
    }

    public event EventHandler? Clicked;
    public event EventHandler? Pressed;
    public event EventHandler? LongPressed;
    public event EventHandler? Released;

    public async Task PerformClickableEvent(object sender, ClickableEventArgs e)
    {
        await e;
        if (!e.Handled)
        {
            switch (e.EventType)
            {
                case ClickableEventType.Pressed:
                    Pressed?.Invoke(this, e);
                    break;
                case ClickableEventType.Clicked:
                    Clicked?.Invoke(this, e);
                    break;
                case ClickableEventType.LongPressed:
                    LongPressed?.Invoke(this, e);
                    break;
                case ClickableEventType.Released:
                    Released?.Invoke(this, e);
                    break;
                default:
                    break;
            }
        }
    }
}