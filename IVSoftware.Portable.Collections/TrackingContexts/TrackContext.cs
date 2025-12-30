using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.SQLiteMarkdown.Collections;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections.TrackingContexts
{
    [DebuggerDisplay("Count={CurrentItemsProtected.Count}")]
    public class TrackContext<T> : INotifyPropertyChanged
    {
        public TrackContext(IObservablePreviewCollection owner, string propertyName)
        {
            _owner = owner;
            var pi = typeof(T).GetMostDerivedProperty(propertyName);
            if (pi is null)
            {
                this.ThrowHard<InvalidOperationException>(
                    $"{typeof(T).Name}.{propertyName} cannot be resolved.");
                return;
            }
            PropertyInfo = pi;
            TrackValueDomain = localGetTrackValueDomain();
            if(TrackValueDomain == TrackValueDomain.Incompatible)
            {
                this.ThrowHard<ArgumentNullException>(
                    $"{PropertyInfo.Name} is not a compatible follow property.");

                return;
            }
            // The [Follow] attribute can, for example, invert a bool.
            if (PropertyInfo.GetCustomAttribute<TrackAttribute>() is { } attr)
            {
                TrackMode = attr.Mode;
                Condition = attr.Condition;
            }
            else
            {
                switch (TrackValueDomain)
                {
                    case TrackValueDomain.Binary:
                        TrackMode = TrackMode.Multiple;
                        Condition = WherePredicate.IsTrue;
                        break;
                    case TrackValueDomain.Stateful:
                        TrackMode = TrackMode.Single;
                        Condition = WherePredicate.IsNotZero;
                        break;
                    case TrackValueDomain.Incompatible:
                    default:
                        this.ThrowHard<NotSupportedException>($"The {TrackValueDomain.ToFullKey()} case is not supported.");
                        return;
                }
            }

            // These options aren't options anymore.
            // Set this here, after validation prologue.
            owner.OptimizationMode |= 
                ListOptimizationMode.UseCacheForContains 
                | ListOptimizationMode.TrackItemPropertyChanges;

            ResetSync(); // Synchronous
            
            if (owner is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += (sender, e) =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            if (e.NewItems is not null)
                            {
                                foreach (T item in e.NewItems)
                                {
                                    if (_compiledPredicate(item))
                                    {
                                        CurrentItemsProtected.Add(item);
                                    }
                                    else
                                    {
                                        CurrentItemsProtected.Remove(item);
                                    }
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            if (e.OldItems is not null)
                            {
                                foreach (T item in e.OldItems)
                                {
                                    CurrentItemsProtected.Remove(item);
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Replace:
                            if (e.OldItems is not null)
                            {
                                foreach (T item in e.OldItems)
                                {
                                    CurrentItemsProtected.Remove(item);
                                }
                            }
                            if (e.NewItems is not null)
                            {
                                foreach (T item in e.NewItems)
                                {
                                    if (_compiledPredicate(item))
                                    {
                                        CurrentItemsProtected.Add(item);
                                    }
                                    else
                                    {
                                        CurrentItemsProtected.Remove(item);
                                    }
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Move:
                            // Noop
                            break;

                        case NotifyCollectionChangedAction.Reset:
                            ResetSync();
                            break;

                        default:
                            break;
                    }
                };
            }

            if (owner is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += (sender, eUnk) =>
                {
                    if (eUnk.PropertyName == PropertyInfo.Name
                        && eUnk is ItemPropertyChangedEventArgs e
                        && e.Item is T item)
                    {
                        if (_compiledPredicate(item))
                        {
                            CurrentItemsProtected.Add(item);
                        }
                        else
                        {
                            CurrentItemsProtected.Remove(item);
                        }
                    }
                };
            }

            #region L o c a l F x
            TrackValueDomain localGetTrackValueDomain()
            {
                var type = PropertyInfo.PropertyType;
                type = Nullable.GetUnderlyingType(type) ?? type;

                // Binary domain.
                if (type == typeof(bool))
                {
                    return TrackValueDomain.Binary;
                }

                // Enum domain must be numerically compatible with FollowState.
                if (type.IsEnum)
                {
                    foreach (var canonical in Enum.GetValues<TrackState>())
                    {
                        if (Enum.TryParse(
                                enumType: type,
                                value: canonical.ToString(),
                                out var parsed)
                            && Convert.ToUInt64(parsed) == Convert.ToUInt64(canonical))
                        {
                            // G T K - numerically compatible
                        }
                        else
                        {
                            return TrackValueDomain.Incompatible;
                        }
                    }
                    return TrackValueDomain.Stateful;
                }

                // Plain integral types are admissible but treated as stateful.
                if (type == typeof(int)
                    || type == typeof(byte)
                    || type == typeof(sbyte)
                    || type == typeof(short)
                    || type == typeof(ushort))
                {
                    return TrackValueDomain.Stateful;
                }

                return TrackValueDomain.Incompatible;
            }
            #endregion

        }

        IList _owner;


        public void ResetSync()
        {
            CurrentItemsProtected.Clear();
            foreach (T item in _owner)
            {
                if (_compiledPredicate(item))
                {
                    CurrentItemsProtected.Add(item);
                }
            }
        }

        public PropertyInfo PropertyInfo
        {
            get => _propertyInfo;
            init
            {
                if (value is null)
                {
                    this.ThrowHard<ArgumentNullException>(
                        $"The {nameof(PropertyInfo)} argument cannot be null.");
                    return;
                }

                _propertyInfo = value;
                var type = Nullable.GetUnderlyingType(_propertyInfo.PropertyType) ?? _propertyInfo.PropertyType;

                if (type == typeof(bool))
                {
                    _compiledGetTrackState =
                        item => ((bool)(_propertyInfo.GetValue(item) ?? false)) ? 1 : 0;
                }
                else if (type.IsEnum)
                {
                    _compiledGetTrackState =
                        item => Convert.ToInt32(_propertyInfo.GetValue(item)!);
                }
                else
                {
                    // int, byte, sbyte, short, ushort
                    _compiledGetTrackState =
                        item => Convert.ToInt32(_propertyInfo.GetValue(item) ?? 0);
                }
            }
        }

        PropertyInfo _propertyInfo = null!;
        Func<T, int> _compiledGetTrackState = null!;


        /// <summary>
        /// Exposes a stable snapshot of the current selection.
        /// </summary>
        /// <remarks>
        /// The returned array is lazily rebuilt only when the underlying selection changes.
        /// This guarantees a consistent, iteration-safe view without exposing live mutation.
        /// </remarks>
        public T[] CurrentItems
        {
            get
            {
                if (_currentItemsDirty)
                {
                    var currentItemsVisible = new HashSet<T>(_owner.Cast<T>());

                    _currentItems = 
                        CurrentItemsProtected
                        .Where(_=>currentItemsVisible.Contains(_))
                        .ToArray();
                    _currentItemsDirty = false;
                }
                return _currentItems;
            }
        }

        T[] _currentItems = [];
        bool _currentItemsDirty = false;

        /// <summary>
        /// Holds the authoritative, mutable selection set.
        /// </summary>
        /// <remarks>
        /// This collection is the source of truth for selection state.
        /// Any structural change marks the public snapshot as dirty, deferring materialization
        /// until the next access.
        /// </remarks>
        protected ObservableHashSet<T> CurrentItemsProtected
        {
            get
            {
                if (_currentItemsProtected is null)
                {
                    _currentItemsProtected = new ObservableHashSet<T>();
                    _currentItemsProtected.CollectionChanged += (sender, e) =>
                    {
                        // [Careful]
                        // Specifically, do *not* respond to Reset which tends to be circular.
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                            case NotifyCollectionChangedAction.Remove:
                                _currentItemsDirty = true;
                                // WDTSettle.StartOrRestart();
                                OnPropertyChanged(nameof(CurrentItems));
                                break;
                            default:
                                break;
                        }
                    };
                }
                return _currentItemsProtected;
            }
        }
        ObservableHashSet<T>? _currentItemsProtected = null;

        public T[] CurrentItemsB
        {
            get
            {
                if (_currentItemsDirty)
                {
                    _ = CurrentItems;
                }
                return _currentItemsB;
            }
        }
        T[] _currentItemsB = [];
#if false
        /// <summary>
        /// TrackInversions.
        /// </summary>
        protected ObservableHashSet<T> CurrentItemsProtectedB
        {
            get
            {
                if (_currentItemsProtectedB is null)
                {
                    _currentItemsProtectedB = new ObservableHashSet<T>();
                    _currentItemsProtectedB.CollectionChanged += (sender, e) =>
                    {
                        _currentItemsDirty = true;
                    };
                }
                return _currentItemsProtectedB;
            }
        }
        ObservableHashSet<T>? _currentItemsProtectedB = null;
#endif

        public void ItemPress(T item)
        {
            if (TrackMode != 0)
            {
                PressedItem = item;
            }
        }

        public void ItemRelease(T item)
        {
            PressedItem = default;  // Different than Current!
            switch (TrackValueDomain)
            {
                case TrackValueDomain.Binary:
                    localItemReleaseBool();
                    break;
                case TrackValueDomain.Stateful:
                    localItemReleaseState();
                    break;
                case TrackValueDomain.Incompatible:
                default:

                    this.ThrowHard<NotSupportedException>($"The {TrackValueDomain.ToFullKey()} case is not supported.");
                    break;
            }
            OnPropertyChanged(nameof(CurrentItems));

            #region L o c a l F x 
            void localItemReleaseState()
            {
                var unk = PropertyInfo?.GetValue(item);
                TrackState oldState;
                if (unk is not Enum @enum)
                {
                    // Initial validation will have already
                    // thrown in this case. Just avoid use.
                    return;
                }

                oldState = (TrackState)@enum;

                var others = CurrentItems.Where(_ => !ReferenceEquals(_, item)).ToArray();

                TrackMode followMode;
                string modifiers = string.Empty;
                bool isDemote = false;
                if (TrackMode == TrackMode.Single)
                {
                    var e = new ModifiersRequestEventArgs();
                    ModifiersRequest?.Invoke(this, e);
                    modifiers = string.Join(" | ", e.Modifiers.Select(_ => _.Trim().ToLower()).OrderBy(_ => _));

                    // Specific modifiers cause temporary elevation.
                    switch (modifiers)
                    {
                        case "control":
                            followMode = TrackMode.Multiple;
                            break;
                        case "alt | control":
                            followMode = TrackMode.Multiple;
                            isDemote = true;
                            break;
                        default:
                            followMode = TrackMode;
                            break;
                    }
                }
                else
                {
                    followMode = TrackMode;
                }

                switch (followMode)
                {
                    case TrackMode.None:
                        SetItemState(item, TrackState.None);
                        break;
                    case TrackMode.Single:
                        switch (oldState)
                        {
                            case TrackState.None:
                            case TrackState.Multi:    // Promote
                            case TrackState.Primary:  // Promote
                                SetItemState(item, TrackState.Exclusive);
                                foreach (var oldSel in CurrentItemsProtected.ToArray())
                                {
                                    if (!ReferenceEquals(oldSel, item))
                                    {
                                        SetItemState(oldSel, TrackState.None);
                                    }
                                }
                                break;
                            case TrackState.Exclusive:
                                SetItemState(item, TrackState.None);
                                break;
                        }
                        break;
                    case TrackMode.Multiple:
                        if (others.Any())
                        {
                            switch (oldState)
                            {
                                case TrackState.Primary:
                                    if (isDemote)
                                    {
                                        SetItemState(item, TrackState.Multi);
                                    }
                                    else
                                    {
                                        SetItemState(item, TrackState.None);
                                    }
                                    break;
                                default:
                                    SetItemState(item, TrackState.Primary);
                                    foreach (var oldSel in CurrentItemsProtected.ToArray())
                                    {
                                        if (!ReferenceEquals(oldSel, item))
                                        {
                                            SetItemState(oldSel, TrackState.Multi);
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            switch (oldState)
                            {
                                case TrackState.None:
                                    SetItemState(item, TrackState.Exclusive);
                                    break;
                                default:
                                    SetItemState(item, TrackState.None);
                                    break;
                            }
                        }
                        break;
                    default:
                        break;
                }
                // Case:
                // 1. Primary + Multi in list.
                // 2. The Primary goes away
                if (CurrentItemsProtected.Count == 1)
                {
                    T item = CurrentItemsProtected.Cast<T>().First();
                    if (GetItemState(item) != TrackState.Exclusive)
                    {
                        SetItemState(item, TrackState.Exclusive);
                    }
                }
                OnPropertyChanged(nameof(CurrentItems));
            }

            void localItemReleaseBool()
            {
                var next = _compiledGetTrackState(item) == 0 ? 1 : 0;
                PropertyInfo.SetValue(item, next);
            }
            #endregion L o c a l F x
        }

        TrackState GetItemState(T item) => (TrackState)_compiledGetTrackState(item);
        void SetItemState(T item, TrackState newState)
        {
            PropertyInfo.SetValue(item, newState);
            if (_compiledPredicate(item))
            {
                CurrentItemsProtected.Add(item);
            }
            else
            {
                CurrentItemsProtected.Remove(item);
            }
        }

        public TrackMode TrackMode
        {
            get => _trackMode;
            set
            {
                if (!Equals(_trackMode, value))
                {
                    _trackMode = value;
                    OnPropertyChanged();
                }
            }
        }
        TrackMode _trackMode = TrackMode.Single;

        public WherePredicate Condition
        {
            get => _condition;
            set
            {
                if (!Equals(_condition, value))
                {
                    _condition = value;
                    OnConditionChanged();
                    OnPropertyChanged();
                }
            }
        }

        private void OnConditionChanged()
        {
            var getState = _compiledGetTrackState;

            _compiledPredicate = Condition switch
            {
                WherePredicate.IsNotZero => item => getState(item) != 0,
                WherePredicate.IsZero => item => getState(item) == 0,
                WherePredicate.IsLessThanZero => item => getState(item) < 0,
                WherePredicate.IsGreaterThanZero => item => getState(item) > 0,
                WherePredicate.IsLessThanOrEqualToZero => item => getState(item) <= 0,
                WherePredicate.IsGreaterThanOrEqualToZero => item => getState(item) >= 0,
                WherePredicate.IsTrue => item => getState(item) != 0,
                WherePredicate.IsFalse => item => getState(item) == 0,
                _ => throw new NotSupportedException(
                    $"Unsupported {nameof(WherePredicate)}: {Condition}")
            };
            ResetSync();
        }


        WherePredicate _condition = (WherePredicate)(-1);
        Func<T, bool> _compiledPredicate = null!;

        public T? PressedItem
        {
            get => _pressedItem;
            set
            {
                if (!Equals(_pressedItem, value))
                {
                    _pressedItem = value;
                    OnPropertyChanged();
                }
            }
        }
        T? _pressedItem = default;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        internal void SyncReset() => OnPropertyChanged(nameof(CurrentItems));

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ModifiersRequestEventArgs>? ModifiersRequest;

        WatchdogTimer WDTSettle
        {
            get
            {
                if (_wdtSettle is null)
                {
                    _wdtSettle = new WatchdogTimer(
                        defaultInitialAction: () =>
                    {
                        throw new NotImplementedException("ToDo");
                    },
                    defaultCompleteAction: () =>
                    {
                    });
                    _wdtSettle.Interval = TimeSpan.FromSeconds(0.1);
                }
                return _wdtSettle;
            }
        }
        WatchdogTimer? _wdtSettle = null;

        internal TrackValueDomain TrackValueDomain { get; }
    }
}