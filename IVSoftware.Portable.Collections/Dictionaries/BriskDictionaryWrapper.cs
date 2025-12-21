using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static IVSoftware.Portable.Collections.Framework;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    public sealed class BriskDictionaryWrapper 
    {
        public BriskDictionaryWrapper(XElement xdunk, Type? template = null)
        {
            XDUNK = xdunk;
            XBA = new XBoundAttribute(
                name: nameof(StdCollectionXAttribute.dunk),
                tag: this,
                text: Name); // Placeholder.
            XDUNK.Add(XBA);
            template ??= typeof(TolerantDictionary<object, object>);

            if (!template.IsAssignableTo(typeof(IObservableDictionary)))
            { 
                this.ThrowHard<ArgumentException>($"{nameof(template)} type must be assignable to {nameof(IObservableDictionary)}");
                return;
            }
            @base = (IObservableDictionary)Activator.CreateInstance(template)!;
        }
        /// <summary>
        /// Immutable node in the model.
        /// </summary>
        public XElement XDUNK { get; }
        public XBoundAttribute XBA { get; }
        public IObservableDictionary @base
        {
            get
            {
                return _base;
            }
            set
            {
                if (value is not null && !Equals(_base, value))
                {
                    if(@base is not null)
                    {
                        @base.CollectionChanged -= OnCollectionUpdate;
                        if(BriskDictionary.ReverseLookup.ContainsKey(@base))
                        {
                            BriskDictionary.ReverseLookup.Remove(@base);
                        }
                    }
                    _base = value;
                    UpdateStrongTypesInfo();
                    OnPropertyChanged();
                    if (@base is not null)
                    {
                        BriskDictionary.ReverseLookup[@base] = this;
                        @base.CollectionChanged += OnCollectionUpdate;
                    }
                }
            }
        }
        IObservableDictionary _base = null!;
        
        private void UpdateStrongTypesInfo()
        {
            var type = @base.GetType();
            var genericArgs =
                type.IsGenericType
                ? type.GetGenericArguments()
                : [];
            if (genericArgs.Length == 2)
            {
                Name = $"[{genericArgs[0].Name}🡒{genericArgs[1].Name}]";
            }
            else
            {
                Name = $"[{typeof(object).Name}🡒{typeof(object).Name}]";
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (!Equals(_name, value))
                {
                    _name = value;
                    OnPropertyChanged();    // Handles the XBA update
                }
            }
        }
        string _name = "[Unnamed Dictionary]";
        public override string ToString() 
            => $"{Name} Count={@base.Count:D2}";
        public void AddRange(IEnumerable<DictionaryEntryPreview> entries)
        {
            @base.AddRange(entries);
        }

        internal bool TryStrongTypesUpgrade<TKey, TValue>(
            DictionaryMode? mode,
            out IObservableDictionary<TKey, TValue>? stronglyTyped,
            out StrongTypesUpgradeStatus status,
            bool @throw = false)
            where TKey : notnull
        {
            var localResult = StrongTypesUpgradeStatus.NoChangeNeeded;
            mode ??= @base.Mode;

            // If the mode is the same *after* null coalescing
            // then it qualifies for the next fast track check.
            stronglyTyped = 
                (mode == @base.Mode)
                ? @base as IObservableDictionary<TKey, TValue>
                : null;

            if (stronglyTyped is null)
            {
                IObservableDictionary upgrade;

                switch ((DictionaryMode)mode)
                {
                    case DictionaryMode.TolerantReturnDefault:
                    case DictionaryMode.TolerantCreateDefaultEntry:
                        upgrade = new TolerantDictionary<TKey, TValue>((DictionaryMode)mode)!;
                        break;
                    case DictionaryMode.InsistentNotNull:
                        upgrade = new InsistentDictionary<TKey, TValue>()!;
                        break;
                    case DictionaryMode.Brisk:
                        if (@throw)
                        {
                            status = StrongTypesUpgradeStatus.RequestedModeIsNotStrongTyped;
                            this.ThrowHard<InvalidOperationException>($"The requested mode is '{mode.ToFullKey()}' which is not strongly typed.");
                        }
                        goto breakFromInner;
                    case DictionaryMode.Normal:
                    default:
                        upgrade = new ObservableDictionary<TKey, TValue>();
                        break;
                }

                if (@base.Keys.Count > 0)
                {
                    Type
                        typeKey = typeof(TKey),
                        typeValue = typeof(TValue);
                    foreach (var key in @base.Keys)
                    {
                        if (key.GetType().IsAssignableTo(typeKey))
                        {
                            var value = @base[key];
                            if (value is null)
                            {
                                if (mode == DictionaryMode.InsistentNotNull)
                                {
                                    localResult = StrongTypesUpgradeStatus.IncompatibleTValue;
                                    goto breakFromInner;
                                }
                                else
                                {
                                    // Tolerant or normal modes can carry null values.
                                    upgrade[key] = default!;
                                }
                            }
                            else
                            {
                                if (value.GetType().IsAssignableTo(typeValue))
                                {
                                    upgrade[key] = value;
                                }
                                else
                                {
                                    localResult = StrongTypesUpgradeStatus.IncompatibleTValue;
                                    goto breakFromInner;
                                }
                            }
                        }
                        else
                        {
                            localResult = StrongTypesUpgradeStatus.IncompatibleTKey;
                            goto breakFromInner;
                        }
                    }
                }

                localResult = StrongTypesUpgradeStatus.Succeeded;

                if(@base is IUpgradeableDictionary upgradeable)
                {
                    upgradeable.TransferEvents(to: upgrade);
                }

                // [Careful]
                // DFWI - @base activly syncs itself with the _reverse lookup.
                @base = upgrade;

                stronglyTyped = @base as IObservableDictionary<TKey, TValue>;
                if (stronglyTyped is null)
                {
                    this.ThrowHard<InvalidCastException>(
                        $"IFD Error. Resulting case is {stronglyTyped?.GetType().ToFormattedTypeName() ?? "null"}");
                }
            }

            breakFromInner:
            status = localResult;
            switch (localResult)
            {
                case StrongTypesUpgradeStatus.NoChangeNeeded:
                case StrongTypesUpgradeStatus.Succeeded:
                    return true;
                case StrongTypesUpgradeStatus.IncompatibleTKey:
                case StrongTypesUpgradeStatus.NotUpgradable:
                    if(@throw)
                    {
                        this.ThrowHard<InvalidOperationException>($"{localResult.ToFullKey()}");
                    }
                    return false;
                default:
                    this.ThrowHard<NotSupportedException>($"Bad case: {localResult.ToFullKey()}");
                    return false;
            }
        }

        private void OnCollectionUpdate(object? sender, NotifyCollectionChangedEventArgs e)
        {
            XBA.Value = $"{ToString()}";
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            switch (propertyName)
            {
                case nameof(Name):
                    XBA.Value = $"{ToString()}";
                    break;
                default:
                    break;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal static void Vacuum()
        {
            BriskDictionary.ReverseLookup.Clear();
            foreach (
                var bdw in 
                Brisk.Model
                .Descendants().Select(_=>_.To<BriskDictionaryWrapper>())
                .OfType<BriskDictionaryWrapper>())
            {
                BriskDictionary.ReverseLookup[bdw.@base] = bdw;
            }
        }

        internal event PropertyChangedEventHandler? PropertyChanged;


        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => @base.CollectionChanged += value;
            remove => @base.CollectionChanged -= value;
        }

        public event NotifyCollectionChangingEventHandler? CollectionChanging
        {
            add => @base.CollectionChanging += value;
            remove => @base.CollectionChanging -= value;
        }
    }
}
