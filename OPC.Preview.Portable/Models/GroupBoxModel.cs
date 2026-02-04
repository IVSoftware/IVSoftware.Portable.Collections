
using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace OPC.Preview.Portable.Models
{
    /// <summary>
    /// Binding context for GroupBoxItemView template.
    /// </summary>
    /// <remarks>
    /// Intended for use as a templated item for ConfigurableCollectionView UI controls.
    /// </remarks>
    public class GroupBoxModel
        : ModalItemBaseModel
        , IOPConfigurable
        , IOPAmbientBindingContext
        , IOPSettingsSink
    {
        public static GroupBoxModel Create<T>(
            ISettingsSource? settings = null)
            where T : struct, Enum
            => Create(typeof(T), settings);

        [Canonical]
        public static GroupBoxModel Create(
            Type type,
            ISettingsSource? settings = null)
        {
            if (type.IsEnum)
            {
                var @new = new GroupBoxModel(settings);
                @new.Configure(type);
                return @new;
            }
            else
            {
                "GroupBoxModel.Create".ThrowHard<NotSupportedException>($"Expecting Enum type.");
                return null!;
            }
        }

        public GroupBoxModel(ISettingsSource? settings = null)
        {
            Settings = settings;
            Items.AmbientBindingContext = this;
        }
        public string GroupName { get; private set; } = string.Empty;

        public Type? Configuration
        {
            get => _configuration;
            set
            {
                if (!Equals(_configuration, value))
                {
                    _configuration = value;
                    OnConfigurationChanged();
                    OnPropertyChanged();
                }
            }
        }
        Type? _configuration = default;

        public void Configure<T>() => Configuration = typeof(T);
        public void Configure(Type? type) => Configuration = type;

        public ObservablePreviewCollection<object> Items
        {
            get
            {
                if (_items is null)
                {
                    _items = new ObservablePreviewCollection<object>();
                }
                return _items;
            }
        }

        ObservablePreviewCollection<object>? _items = null;

        public ISettingsSource? Settings
        {
            get => _settings;
            set
            {
                if (!Equals(_settings, value))
                {
                    _settings = value;
                    OnPropertyChanged();
                }
            }
        }
        public object? AmbientBindingContext
        {
            get => _ambientBindingContext;
            set
            {
                if (!Equals(_ambientBindingContext, value))
                {
                    _ambientBindingContext = value;
                    OnPropertyChanged();
                }
            }
        }
        object? _ambientBindingContext = default;

        ISettingsSource? _settings = null;

        protected virtual void OnConfigurationChanged()
        {
            if (Configuration?.IsEnum == true)
            {
                GroupBoxItemStyle style;
                if(Configuration.GetCustomAttribute<GroupAttribute>() is { } attrGroup)
                {
                    style = attrGroup.Style;
                    // - Explicit null: Use config type name.
                    // - Explicit empty: Hide label entirely.
                    // - Explicit non-empty: Custom group name.
                    GroupName =
                        attrGroup.Name is null
                        ? Configuration.Name
                        : string.IsNullOrWhiteSpace(attrGroup.Name)
                            ? string.Empty
                            : attrGroup.Name;
                }
                else
                {
                    style = GroupBoxItemStyle.String;
                    GroupName = Configuration.Name;
                }
                // [Careful]
                // - This relies on a mapping in the Settings ctor.
                // - It will *not* detect changes e.g. ShowCheckedState.All -> ShowCheckedStateGroup.All
                // - So to *TROUBLESHOOT* check the inits {1F869F84-35E1-4345-B652-0063DFCC1F0A}
                //      this[StdSetting.ShowCheckedStateGroup] = ShowCheckedStateGroup.All;
                Enum? activeMember = Settings?[Configuration.Name].SafeAs<Enum>();
                List<object> items = new();
                foreach (Enum member in Enum.GetValues(Configuration))
                {
                    Enum? glyphKey = member.GetCustomAttribute<GlyphAttribute>()?.StdEnum;
                    var item = new GroupBoxItemModel(member, style, Items);
                    items.Add(item);
                    if (Equals(member, activeMember))
                    {
                        item.IsChecked = true;
                    }
                    else
                    {
                        item.IsChecked = false;
                    }
                }
                Items.Clear();
                Items.AddRange(items);
            }
            else
            {
                this.ThrowHard<InvalidOperationException?>($"{nameof(Configure)}() requires T is Enum.");
            }
        }
    }
}
