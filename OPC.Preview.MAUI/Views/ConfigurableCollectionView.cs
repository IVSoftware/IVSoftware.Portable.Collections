using IVSoftware.Portable;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.Lists;
using OPC.Preview.Maui.Converters;
using OPC.Preview.Portable;
using OPC.Preview.Portable.Models;
using System.Reflection;
using IVSoftware.Portable.Collections;
using System.Diagnostics;
using IVSoftware.Portable.Disposable;
using System.Windows.Input;
using System.Collections.Specialized;

namespace OPC.Preview.Maui.Views
{
    public class ConfigurableCollectionView
        : CollectionView
        , IMultiConfigurable
        , ISettingsSink
    {
        public ConfigurableCollectionView()
        {            
            Debug.Assert(ItemSizingStrategy == ItemSizingStrategy.MeasureAllItems);
            SelectionMode = SelectionMode.None;
            var pgr = new PointerGestureRecognizer();
            pgr.PointerMoved += (sender, e) => OnPointerMoved(e);
            GestureRecognizers.Add(pgr);
            ItemTemplate = new ModalTemplateSelector();
        }

        private void OnPointerMoved(PointerEventArgs e)
        {
            PointerMoved?.Invoke(this, e);
        }

        public event EventHandler<PointerEventArgs>? PointerMoved;

        /// <summary>
        /// Populates with arbitrary polymorphic
        /// models based on Type configuration.
        /// </summary>
        public ObservablePreviewCollection<object> Items
        {
            get
            {
                if (_items is null)
                {
                    _items = new ObservablePreviewCollection<object>();
                    ItemsSource = _items;                    
                }
                return _items;
            }
        }
        ObservablePreviewCollection<object>? _items = null;


        public void Configure<T>() => Configure(typeof(T));
        public void Configure(Type? type) => Configuration = type;

        [Canonical]
        public void Configure(params Type[] types) => MultiConfiguration = types;

        public static readonly BindableProperty ConfigurationProperty =
                BindableProperty.Create(
                    propertyName: nameof(Configuration),
                    returnType: typeof(Type),
                    declaringType: typeof(ConfigurableCollectionView),
                    defaultValue: default,
                    defaultBindingMode: BindingMode.OneWay,
                    propertyChanged: (bindable, oldValue, newValue) =>
                    {
                        if (bindable is ConfigurableCollectionView @this)
                        {
                            if (@this.DHostConfigSource.IsZero())
                            {
                                @this.MultiConfiguration =
                                    @this.Configuration is null
                                    ? []
                                    : [@this.Configuration];
                            }
                        }
                    });

        public Type? Configuration
        {
            get => (Type?)GetValue(ConfigurationProperty);
            set => SetValue(ConfigurationProperty, value);
        }

        public static readonly BindableProperty MultiConfigurationProperty =
                BindableProperty.Create(
                    propertyName: nameof(MultiConfiguration),
                    returnType: typeof(Type[]),
                    declaringType: typeof(ConfigurableCollectionView),
                    defaultValue: Array.Empty<Type>(),
                    defaultBindingMode: BindingMode.OneWay,
                    propertyChanged: (bindable, oldValue, newValue) =>
                    {
                        if (bindable is ConfigurableCollectionView @this)
                        {
                            @this.OnConfigurationChanged();
                        }
                    });

        public Type[] MultiConfiguration
        {
            get => (Type[])GetValue(MultiConfigurationProperty);
            set
            {
                value ??= Array.Empty<Type>();
                using (DHostConfigSource.GetToken(sender: nameof(MultiConfiguration)))
                {
                    SetValue(
                        MultiConfigurationProperty,
                        value);
                    SetValue(
                        ConfigurationProperty,
                        value.Length == 1
                        ? value[0]
                        : null);
                }
            }
        }
        private Type[] _configurationCache = [];

        /// <summary>
        /// Arbitration object. 
        /// </summary>
        /// <remarks>
        /// Configuration forwards to MultiConfiguration unless
        /// it's MultiConfiguration that's updating Configuration.
        /// </remarks>
        public DisposableHost DHostConfigSource
        {
            get
            {
                if (_dhostConfigSource is null)
                {
                    _dhostConfigSource = new DisposableHost();
                    _dhostConfigSource.FinalDispose += (sender, e) =>
                    {
                        var rs = e.ReleasedSenders;
                    };
                }
                return _dhostConfigSource;
            }
        }
        DisposableHost? _dhostConfigSource = null;
        protected virtual void OnConfigurationChanged()
        {
            if (!_configurationCache.SequenceEqual(MultiConfiguration))
            {
                List<object> items = new();
                foreach (var type in MultiConfiguration)
                {
                    if (type.GetCustomAttribute<GroupAttribute>() is { } groupAttr)
                    {
                        // The Settings property is bound on MainPage for
                        // ModalCollectionView control and forwarded in here.
                        var model = GroupBoxModel.Create(type, Settings);
                        //model.Container.ContainerCommand = IExecuteContainerCommand;
                        items.Add(model);
                    }
                    else
                    {
                        foreach (Enum item in Enum.GetValues(type))
                        {
                            if (item.GetCustomAttribute<GlyphAttribute>() is { } glyphAttr)
                            {
                                items.Add(new GlyphButtonModel(stdIconName: glyphAttr.StdEnum));
                            }
                            else
                            {
                                items.Add(item.ToString());
                            }
                        }
                    }
                }                
                Items.Clear();
                Items.AddRange(items);

                // [Careful]
                // Cache the sequence, not the reference.
                _configurationCache = MultiConfiguration.ToArray();
            }
        }


        public static readonly BindableProperty SettingsProperty =
            BindableProperty.Create(
                propertyName: nameof(Settings),
                returnType: typeof(ISettingsSource),
                declaringType: typeof(ConfigurableCollectionView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is ConfigurableCollectionView @this)
                    {
                        // Do something with @this.Settings
                    }
                });

        public ISettingsSource Settings
        {
            get => (ISettingsSource)GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }


        public static readonly BindableProperty ClickableEventCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(ClickableEventCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(ConfigurableCollectionView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is ConfigurableCollectionView @this)
                    {
                        // Do something with @this.ClickableEventCommand
                    }
                });

        public ICommand ClickableEventCommand
        {
            get => (ICommand)GetValue(ClickableEventCommandProperty);
            set => SetValue(ClickableEventCommandProperty, value);
        }
    }
}
