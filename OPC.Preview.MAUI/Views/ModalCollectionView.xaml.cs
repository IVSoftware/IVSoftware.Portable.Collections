using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.Lists;
using OPC.Preview.Portable;
using OPC.Preview.Portable.Models;
using System.Reflection;
using System.Windows.Input;

namespace OPC.Preview.Maui.Views;

[Group]
public enum DCP
{
    Dogs,
    Cats,
    Pets,
}

/// <summary>
/// Lightweight wrapper centers a ConfigurableCollectionView.
/// </summary>
public partial class ModalCollectionView
    : ContentView
    , IMultiConfigurable
{
    public ModalCollectionView()
    {
        InitializeComponent();
        IsVisible = false;
        ModalResultCommittedEventArgs.ModalResultCommitted += (sender, e) =>
        {
            if(e.EndModal)
            {
                IsVisible = false;
            }
        };
    }

    public void Configure<T>() => CollectionViewInternal.Configure<T>();
    public void Configure(Type? type) => CollectionViewInternal.Configure(type);

    public static readonly BindableProperty ConfigurationProperty =
            BindableProperty.Create(
                propertyName: nameof(Configuration),
                returnType: typeof(Type),
                declaringType: typeof(ModalCollectionView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is ModalCollectionView @this)
                    {
                        var config = newValue as Type;
                        @this.CollectionViewInternal.Configuration = newValue as Type;
                        @this.IsVisible = config is not null;
                    }
                });

    public Type? Configuration
    {
        get => CollectionViewInternal.Configuration;
        set => CollectionViewInternal.Configuration = value;
    }

    public static readonly BindableProperty MultiConfigurationProperty =
            BindableProperty.Create(
                propertyName: nameof(MultiConfiguration),
                returnType: typeof(Type[]),
                declaringType: typeof(ModalCollectionView),
                defaultValue: Array.Empty<Type>(),
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is ModalCollectionView @this)
                    {
                        var config = newValue as Type[] ?? [];
                        @this.CollectionViewInternal.MultiConfiguration = config;
                        @this.IsVisible = config.Length > 0;
                    }
                });


    public Type[] MultiConfiguration
    {
        get => CollectionViewInternal.MultiConfiguration;
        set => CollectionViewInternal.MultiConfiguration = value;
    }
    private Type[] _configurationCache = [];
    private void OnOverlayTapped(object sender, TappedEventArgs e)
    {
        this.SetModalResult(ModalResult.Cancel);
    }

    public void Configure(params Type[] types) => MultiConfiguration = types;

    public static readonly BindableProperty SettingsProperty =
        BindableProperty.Create(
            propertyName: nameof(Settings),
            returnType: typeof(ISettingsSource),
            declaringType: typeof(ModalCollectionView),
            defaultValue: default,
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is ModalCollectionView @this
                    && newValue is ISettingsSource settings)
                {
                    @this.CollectionViewInternal.Settings = settings;
                }
            });

    public ISettingsSource Settings
    {
        get => CollectionViewInternal.Settings;
        set => CollectionViewInternal.Settings = value;
    }


    public static readonly BindableProperty ClickableEventCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(ClickableEventCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(ModalCollectionView),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is ModalCollectionView @this)
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
