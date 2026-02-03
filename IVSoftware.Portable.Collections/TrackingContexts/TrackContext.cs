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
    /// <summary>
    /// Maintains a live, property-driven subset of an ObservablePreviewCollection.
    /// </summary>
    /// <remarks>
    /// A TrackContext observes a single property on items of type T and incrementally
    /// maintains a synchronized subset whose membership is determined by a predicate.
    /// 
    /// Membership updates are driven by both collection mutations and item property
    /// changes, allowing selection state to be modeled in the data layer rather than
    /// in platform-specific views.
    /// 
    /// Track contexts are commonly instantiated implicitly via the [Track] attribute.
    /// When activated, the owning collection enables the required optimization modes
    /// and routes item property changes through the tracking pipeline.
    /// 
    /// The public snapshot exposed by this type is stable and iteration-safe, providing
    /// a consistent view of the current logical subset without exposing live mutation.
    /// </remarks>

    [DebuggerDisplay("{PropertyInfo?.Name} Count={CurrentItemsProtected.Count}")]
    public class TrackContext<T> : ITrackContext
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

            // These options are no longer optional.
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
                        // [Probationary]
                        // Reset might be circular.
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                            case NotifyCollectionChangedAction.Remove:
                            case NotifyCollectionChangedAction.Reset:
                                _currentItemsDirty = true;
                                WDTCurrentItemsChangeSettled.StartOrRestart();
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

        public string Modifiers
        {
            get
            {
                var e = new ModifiersRequestEventArgs();
                ModifiersRequest?.Invoke(this, e);
                return string.Join(" | ", e.Modifiers.Select(_ => _.Trim().ToLower()).OrderBy(_ => _));
            }
        }

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

        public void ItemPressed(T item)
        {
            if (TrackMode != 0)
            {
                PressedItem = default;
                if(string.IsNullOrWhiteSpace(Modifiers))
                {
                    foreach (var other in CurrentItems)
                    {
                        if(!ReferenceEquals(other, item))
                        {
                            SetItemState(other, TrackState.None);
                        }
                    }
                }
                PressedItem = item;
            }
        }

        /// <summary>
        /// Indicates a pointer up gesture. 
        /// </summary>
        /// <remarks>
        /// If the item being released is the same as the
        /// captured item pressed then the state will advance.
        /// </remarks>
        public void ItemReleased(T? item)
        {
            if (PressedItem is null || !ReferenceEquals(PressedItem, item))
            {
                // Null check, but also do not toggle unless pointer
                // comes up on the same item that it went down on.
                return;
            }
            else
            {
                var unk = PropertyInfo?.GetValue(PressedItem);
                if (unk is not Enum @enum)
                {
                    // Initial validation will have already
                    // thrown in this case. Just avoid use.
                    return;
                }
                TrackState oldState = (TrackState)@enum;

                item = PressedItem;
                PressedItem = default;
                switch (TrackValueDomain)
                {
                    case TrackValueDomain.Binary:
                        localItemReleaseBool(oldState);
                        break;
                    case TrackValueDomain.Stateful:
                        localItemReleaseState(oldState);
                        break;
                    case TrackValueDomain.Incompatible:
                    default:
                        this.ThrowHard<NotSupportedException>($"The {TrackValueDomain.ToFullKey()} case is not supported.");
                        break;
                }
                WDTCurrentItemsChangeSettled.StartOrRestart();
            }

            void localItemReleaseState(TrackState oldState)
            {
                var others = CurrentItems.Where(_ => !ReferenceEquals(_, item)).ToArray();

                TrackMode followMode;
                bool isDemote = false;
                if (TrackMode == TrackMode.Single)
                {
                    // Specific modifiers cause temporary elevation.
                    switch (Modifiers)
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
                    item = CurrentItemsProtected.Cast<T>().First();
                    if (GetItemState(item) != TrackState.Exclusive)
                    {
                        SetItemState(item, TrackState.Exclusive);
                    }
                }
                WDTCurrentItemsChangeSettled.StartOrRestart();
            }

            void localItemReleaseBool(TrackState oldState)
            {
                var next = _compiledGetTrackState(item) == 0 ? 1 : 0;
                PropertyInfo?.SetValue(item, next);
            }
        }

        public T? PressedItem
        {
            get => _pressedItem;
            protected set
            {
                if (!Equals(_pressedItem, value))
                {
                    if(_pressedItem is not null)
                    {
                        PropertyInfo?.SetValue(_pressedItem, TrackStateEphemeral.NotPressed);
                    }
                    _pressedItem = value;
                    if (_pressedItem is null)
                    {
                        WDTLongPressed.Cancel();
                    }
                    else
                    {
                        WDTLongPressed.StartOrRestart();
                    }
                    OnPropertyChanged();
                }
            }
        }
        T? _pressedItem = default;

        public WatchdogTimer WDTLongPressed
        {
            get
            {
                if (_wdtPressed is null)
                {
                    _wdtPressed = new WatchdogTimer
                    {
                        Interval = TimeSpan.FromSeconds(0.6),
                    };
                    _wdtPressed.RanToCompletion += (sender, _) =>
                    {
                        var e = new LongPressedEventArgs(PressedItem);
                        if (e.Item is not null)
                        {
                            CancelItemPressed();
                            LongPressed?.Invoke(this, e);
                        }
                    };
                }
                return _wdtPressed;
            }
        }
        WatchdogTimer? _wdtPressed = null;

        public WatchdogTimer WDTCurrentItemsChangeSettled
        {
            get
            {
                if (_wdtCurrentItemsChangeSettled is null)
                {
                    _wdtCurrentItemsChangeSettled = new WatchdogTimer(defaultCompleteAction: localOnItemsChangeSettled)
                    {
                        Interval = TimeSpan.FromMilliseconds(10)
                    };
                }
                return _wdtCurrentItemsChangeSettled;

                void localOnItemsChangeSettled()
                {
                    OnPropertyChanged(nameof(CurrentItems));
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        WatchdogTimer? _wdtCurrentItemsChangeSettled = null;

        public event EventHandler<LongPressedEventArgs>? LongPressed;

        public TimeSpan LongPressedDelay
        {
            get => WDTLongPressed.Interval;
            set
            {
                if (!Equals(WDTLongPressed.Interval, value) 
                    && value.TotalSeconds > LONG_PRESSED_MIN_SECONDS)
                {
                    WDTLongPressed.Interval = value;
                    OnPropertyChanged();
                }
            }
        }
        const double LONG_PRESSED_MIN_SECONDS = 0.25;
        public void CancelItemPressed() => PressedItem = default;

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

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        internal void SyncReset() => WDTCurrentItemsChangeSettled.StartOrRestart();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ModifiersRequestEventArgs>? ModifiersRequest;

        internal TrackValueDomain TrackValueDomain { get; }

        public int Count => CurrentItems.Length;

        Array ITrackContext.CurrentItems => CurrentItems;
    }
}