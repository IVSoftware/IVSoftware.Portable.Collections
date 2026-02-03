using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using OPC.Preview.Portable;
using OPC.Preview.Portable.Events;
using OPC.Preview.Portable.Models;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Input;

namespace OPC.Preview.Maui.Views;

/// <summary>
/// Typically the ClickableEventCommand is bound
/// at a high level to handle icon clicks.
/// </summary>
public partial class PropertyEditorView
    : ContentView
    , IOPItemEditor
    , IOPConfigurable
    , IOPClickable
    , IOPClickableSink
{
	public PropertyEditorView()
	{
		InitializeComponent();
		Loaded += (sender, e) => Focus();
		PropertyChanging += (sender, e) =>
		{
			switch (e.PropertyName)
			{
				case nameof(IsVisible):
					if (!IsVisible)
					{
						OnAppearing(EventArgs.Empty);
					}
					break;
			}
		};

        // No need to make it bindable - this is plumbing not configuration.
        ModalCommandBarStack.ClickableEventCommand = new Command<ClickableEventArgs>(OnNestedClickableEvent);
        ModalCommandBarStack.TrackContext = PropertyInfoItems.TrackContexts[nameof(PropertyInfoModel.IsModified)];
    }
    private void OnNestedClickableEvent(ClickableEventArgs e)
    {
        switch (e.EventType)
        {
            case ClickableEventType.Released:
                switch (e.OPID)
                {
                    case ApplyCancel.Apply:
                        foreach (var pii in PropertyInfoItems)
                        {
                            if(pii.IsModified)
                            {
                                pii.Pi.SetValue(Item, pii.Value);
                                pii.IsModified = false;
                            }
                        }
                        break;
                }
                break;
        }
        e = new ClickableEventArgs(this, e);
        ClickableEventCommand?.Execute(e);
    }

    protected virtual void OnAppearing(EventArgs empty)
    {
        bool isFirst = true;
        foreach (var pii in PropertyInfoItems)
        {
            pii.IsFirst = isFirst;
            isFirst = false;
            pii.IsModified = false;
        }
    }

    public static readonly BindableProperty ItemProperty =
        BindableProperty.Create(
            propertyName: nameof(Item),
            returnType: typeof(object),
            declaringType: typeof(PropertyEditorView),
            defaultValue: default,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is PropertyEditorView @this)
                {
                    switch (newValue)
                    {
                        case Type configuration:
                            @this.Configuration = configuration;
                            if(Activator.CreateInstance(configuration) is { } item)
                            {
                                @this.Item = item;
                            }
                            break;
                        default:
                            @this.Configuration = newValue.GetType();
                            foreach (var pii in @this.PropertyInfoItems)
                            {
                                pii.Value = pii.Pi.GetValue(newValue);
                                pii.IsModified = false;
                            }
                            break;
                    }
                }
            });

    public object Item
    {
        get => (object)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public ObservablePreviewCollection<PropertyInfoModel> PropertyInfoItems { get; } = new();

    /// <summary>
    /// Type-based configuration that is very different from
    /// the ApplyCancel modal command bar stack configuration.
    /// </summary>
	public static readonly BindableProperty ConfigurationProperty =
			BindableProperty.Create(
				propertyName: nameof(Configuration),
				returnType: typeof(Type),
				declaringType: typeof(PropertyEditorView),
				defaultValue: default,
				defaultBindingMode: BindingMode.OneWay,
				propertyChanged: (bindable, oldValue, newValue) =>
				{
					if (bindable is PropertyEditorView @this)
					{
						@this.OnConfigurationChanged();
                    }
                });

    public Type? Configuration
	{
		get => (Type?)GetValue(ConfigurationProperty);
		set => SetValue(ConfigurationProperty, value);
	}

	public void Configure<T>() => Configuration = typeof(T);

    public void Configure(Type? type) => Configuration = type;
    protected virtual void OnConfigurationChanged()
    {
        if (Configuration is not null)
        {
            PropertyInfoItems.Clear();

            bool needInit = !Cache.ContainsKey(Configuration);
            IDictionary<string, PropertyInfo> 
                cache =
                    Cache[Configuration]
                    .AsStronglyTypedDictionary<string, PropertyInfo>();
            if(needInit)
            {
                if (Configuration
                    ?.GetCustomAttribute<EditorTemplateAttribute>()?
                    .Template is Type template)
                {
                    var pis =
                        Configuration
                        .GetProperties()
                        .ToDictionary(_ => _.Name, _ => _);
                    foreach (var tpi in template?.GetProperties() ?? [])
                    {
                        if (pis.TryGetValue(tpi.Name, out var pi))
                        {
                            string? key = tpi.GetCustomAttribute<DescriptionAttribute>()?.Description;
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                key = tpi.Name;
                            }
                            cache[key] = pi;
                        }
                        else
                        {
                            var msg = $"{tpi.Name} property in template has no counterpart in configuration.";
                            Debug.Fail(msg);
                            this.Advisory(msg);
                        }
                    }
                }
                else
                {
                    foreach (var pi in Configuration?.GetProperties() ?? [])
                    {
                        cache[pi.Name] = pi;
                    }
                }
            }
            else
            {
                Debug.Fail($@"ADVISORY - First Time - (needed more than one Key in order to test).");
            }
            foreach (var key in cache.Keys)
            {
                PropertyInfoItems.Add(new PropertyInfoModel(key, cache[key]));
            }
            ConfigurationChangedCommand?.Execute(null);
        }
    }


    public static readonly BindableProperty ConfigurationChangedCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(ConfigurationChangedCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(PropertyEditorView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is PropertyEditorView @this)
                    {
                        @this.ConfigurationChangedCommand?.Execute(@this);
                    }
                });

    public ICommand ConfigurationChangedCommand
    {
        get => (ICommand)GetValue(ConfigurationChangedCommandProperty);
        set => SetValue(ConfigurationChangedCommandProperty, value);
    }
    

    /// <summary>
    /// Reflect property info only once per type.
    /// </summary>
    public BriskDictionary Cache
    {
        get
        {
            if (_cache is null)
            {
                _cache = new BriskDictionary();
                _cache.CollectionChanging += (sender, e) =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangingAction.Add:
                            foreach (var entry in e.NewItems?.Cast<DictionaryEntryPreview>() ?? [])
                            {
                                if (entry.Value is IObservableDictionary dict)
                                {
                                    var type = dict.GetType();
                                    if (type.IsGenericType)
                                    {
                                        var args = type.GetGenericArguments();
                                        if (args.Length == 2 &&
                                            args[0] == typeof(string) &&
                                            args[1] == typeof(PropertyInfo))
                                        {
                                            // Short circuit
                                            continue;
                                        }
                                    }
                                }
                                entry.Value = new TolerantDictionary<string, PropertyInfo>();
                            }
                            break;
                    }
                };
                _cache.CollectionChanged += (sender, e) =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
#if DEBUG
                            // Check for critical bug that (worst case) affects BDW host lookup.
                            int 
                                expected = _cache.Model.Descendants().Count(_ => _.Has<BriskDictionaryWrapper>()),
                                actual = _cache.Count;
                            Debug.Assert(
                                expected == actual,
                                $"Detect bug IRL where the @base dictionary did not update.");    
                            foreach(var bdw in _cache.Values.OfType<BriskDictionaryWrapper>())
                            {
                                if (bdw.@base is IDictionary idict)
                                {
                                    if (idict.TryGetHost(out var loopback)
                                        && (ReferenceEquals(loopback, bdw)))
                                    {   /* G T K */
                                    }
                                    else
                                    {
                                        this.ThrowFramework<InvalidOperationException>("Host loopback failed.");
                                    }
                                }
                            }
#endif
                            break;
                    }
                };
            }
            return _cache;
        }
    }

    BriskDictionary? _cache = null;


    public async Task SinkClickableEvent(object sender, ClickableEventArgs e)
    {
		e.Visited.AddDistinct(this);
    }

    #region C L I C K A B L E 
    public static readonly BindableProperty ClickableEventCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(ClickableEventCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(PropertyEditorView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is PropertyEditorView @this)
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
    #endregion C L I C K A B L E

}