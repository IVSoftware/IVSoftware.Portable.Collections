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

namespace IVSoftware.Portable.Collections.FollowContexts
{
    [DebuggerDisplay("Count={CurrentItemsProtected.Count}")]
    public class FollowContext<T> : INotifyPropertyChanged
    {
        public FollowContext(IObservablePreviewCollection owner, string binding)
        {
            _owner = owner;
            PropertyInfo = typeof(T).GetProperty(binding)!;
            FollowValueDomain = localGetFollowValueDomain();
            if(FollowValueDomain == FollowValueDomain.Incompatible)
            {
                this.ThrowHard<ArgumentNullException>(
                    $"{PropertyInfo.Name} is not a compatible follow property.");

                return;
            }
            // The [Follow] attribute can, for example, invert a bool.
            if (PropertyInfo.GetCustomAttribute<FollowAttribute>() is { } attr)
            {
                FollowMode = attr.Mode;
                Condition = attr.Condition;
            }
            else
            {
                switch (FollowValueDomain)
                {
                    case FollowValueDomain.Binary:
                        FollowMode = FollowMode.Multiple;
                        Condition = FollowPredicate.IsTrue;
                        break;
                    case FollowValueDomain.Stateful:
                        FollowMode = FollowMode.Single;
                        Condition = FollowPredicate.IsNotZero;
                        break;
                    case FollowValueDomain.Incompatible:
                    default:
                        this.ThrowHard<NotSupportedException>($"The {FollowValueDomain.ToFullKey()} case is not supported.");
                        return;
                }
            }

            // These options aren't options anymore.
            // Set this here, after validation prologue.
            owner.OptimizationMode |= 
                ListOptimizationMode.UseCacheForContains 
                | ListOptimizationMode.TrackItemPropertyChanges;

            ResetSync();
            if (owner is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += (sender, e) =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            if(e.NewItems is not null)
                            {
                                foreach (T item in e.NewItems)
                                {
                                    if(_compiledPredicate(item))
                                    {
                                        CurrentItemsProtected.Add(item);
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
                    if(eUnk.PropertyName == PropertyInfo.Name && eUnk is ItemPropertyChangedEventArgs e)
                    {
                        if(e.Item is T item)
                        {
                            if(_compiledPredicate(item))
                            {
                                CurrentItemsProtected.Add(item);
                            }
                            else
                            {
                                CurrentItemsProtected.Remove(item);
                            }
                        }
                    }
                };
            }

            #region L o c a l F x
            FollowValueDomain localGetFollowValueDomain()
            {
                var type = PropertyInfo.PropertyType;
                type = Nullable.GetUnderlyingType(type) ?? type;

                // Binary domain.
                if (type == typeof(bool))
                {
                    return FollowValueDomain.Binary;
                }

                // Enum domain must be numerically compatible with FollowState.
                if (type.IsEnum)
                {
                    foreach (var canonical in Enum.GetValues<FollowState>())
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
                            return FollowValueDomain.Incompatible;
                        }
                    }
                    return FollowValueDomain.Stateful;
                }

                // Plain integral types are admissible but treated as stateful.
                if (type == typeof(int)
                    || type == typeof(byte)
                    || type == typeof(sbyte)
                    || type == typeof(short)
                    || type == typeof(ushort))
                {
                    return FollowValueDomain.Stateful;
                }

                return FollowValueDomain.Incompatible;
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
                    _compiledGetFollowState =
                        item => ((bool)(_propertyInfo.GetValue(item) ?? false)) ? 1 : 0;
                }
                else if (type.IsEnum)
                {
                    _compiledGetFollowState =
                        item => Convert.ToInt32(_propertyInfo.GetValue(item)!);
                }
                else
                {
                    // int, byte, sbyte, short, ushort
                    _compiledGetFollowState =
                        item => Convert.ToInt32(_propertyInfo.GetValue(item) ?? 0);
                }
            }
        }

        PropertyInfo _propertyInfo = null!;
        Func<T, int> _compiledGetFollowState = null!;


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
                if(_currentItemsDirty)
                {
                    var currentItemsVisible = new HashSet<T>(_owner.Cast<T>());
                    var currentItems = new List<T>();

                    _currentItems = CurrentItemsProtected.Where(_=>currentItemsVisible.Contains(_)).ToArray();
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
                        _currentItemsDirty = true;
                        WDTSettle.StartOrRestart();
                    };
                }
                return _currentItemsProtected;
            }
        }
        ObservableHashSet<T>? _currentItemsProtected = null;

        public void ItemPress(T item)
        {
            if (FollowMode != 0)
            {
                PressedItem = item;
            }
        }

        public void ItemRelease(T item)
        {
            PressedItem = default;  // Different than Current!
            switch (FollowValueDomain)
            {
                case FollowValueDomain.Binary:
                    localItemReleaseBool();
                    break;
                case FollowValueDomain.Stateful:
                    localItemReleaseState();
                    break;
                case FollowValueDomain.Incompatible:
                default:

                    this.ThrowHard<NotSupportedException>($"The {FollowValueDomain.ToFullKey()} case is not supported.");
                    break;
            }
            OnPropertyChanged(nameof(CurrentItems));

            #region L o c a l F x 
            void localItemReleaseState()
            {
                var unk = PropertyInfo?.GetValue(item);
                FollowState oldState;
                if (unk is not Enum @enum)
                {
                    // Initial validation will have already
                    // thrown in this case. Just avoid use.
                    return;
                }

                oldState = (FollowState)@enum;

                var others = CurrentItems.Where(_ => !ReferenceEquals(_, item)).ToArray();

                FollowMode followMode;
                string modifiers = string.Empty;
                bool isDemote = false;
                if (FollowMode == FollowMode.Single)
                {
                    var e = new ModifiersRequestEventArgs();
                    ModifiersRequest?.Invoke(this, e);
                    modifiers = string.Join(" | ", e.Modifiers.Select(_ => _.Trim().ToLower()).OrderBy(_ => _));

                    // Specific modifiers cause temporary elevation.
                    switch (modifiers)
                    {
                        case "control":
                            followMode = FollowMode.Multiple;
                            break;
                        case "alt | control":
                            followMode = FollowMode.Multiple;
                            isDemote = true;
                            break;
                        default:
                            followMode = FollowMode;
                            break;
                    }
                }
                else
                {
                    followMode = FollowMode;
                }

                switch (followMode)
                {
                    case FollowMode.None:
                        SetItemState(item, FollowState.None);
                        break;
                    case FollowMode.Single:
                        switch (oldState)
                        {
                            case FollowState.None:
                            case FollowState.Multi:    // Promote
                            case FollowState.Primary:  // Promote
                                SetItemState(item, FollowState.Exclusive);
                                foreach (var oldSel in CurrentItemsProtected.ToArray())
                                {
                                    if (!ReferenceEquals(oldSel, item))
                                    {
                                        SetItemState(oldSel, FollowState.None);
                                    }
                                }
                                break;
                            case FollowState.Exclusive:
                                SetItemState(item, FollowState.None);
                                break;
                        }
                        break;
                    case FollowMode.Multiple:
                        if (others.Any())
                        {
                            switch (oldState)
                            {
                                case FollowState.Primary:
                                    if (isDemote)
                                    {
                                        SetItemState(item, FollowState.Multi);
                                    }
                                    else
                                    {
                                        SetItemState(item, FollowState.None);
                                    }
                                    break;
                                default:
                                    SetItemState(item, FollowState.Primary);
                                    foreach (var oldSel in CurrentItemsProtected.ToArray())
                                    {
                                        if (!ReferenceEquals(oldSel, item))
                                        {
                                            SetItemState(oldSel, FollowState.Multi);
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            switch (oldState)
                            {
                                case FollowState.None:
                                    SetItemState(item, FollowState.Exclusive);
                                    break;
                                default:
                                    SetItemState(item, FollowState.None);
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
                    if (GetItemState(item) != FollowState.Exclusive)
                    {
                        SetItemState(item, FollowState.Exclusive);
                    }
                }
                OnPropertyChanged(nameof(CurrentItems));
            }

            void localItemReleaseBool()
            {
                var next = _compiledGetFollowState(item) == 0 ? 1 : 0;
                PropertyInfo.SetValue(item, next);
            }
            #endregion L o c a l F x
        }

        FollowState GetItemState(T item) => (FollowState)_compiledGetFollowState(item);
        void SetItemState(T item, FollowState newState)
        {
            PropertyInfo.SetValue(item, newState);
            switch (newState)
            {
                case FollowState.None:
                    CurrentItemsProtected.Remove(item);
                    break;
                case FollowState.Exclusive:
                case FollowState.Multi:
                case FollowState.Primary:
                    CurrentItemsProtected.Add(item);
                    break;
            }
        }

        public FollowMode FollowMode
        {
            get => _followMode;
            set
            {
                if (!Equals(_followMode, value))
                {
                    _followMode = value;
                    OnPropertyChanged();
                }
            }
        }
        FollowMode _followMode = FollowMode.Single;

        public FollowPredicate Condition
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
            var getState = _compiledGetFollowState;

            _compiledPredicate = Condition switch
            {
                FollowPredicate.IsNotZero => item => getState(item) != 0,
                FollowPredicate.IsZero => item => getState(item) == 0,
                FollowPredicate.IsLessThanZero => item => getState(item) < 0,
                FollowPredicate.IsGreaterThanZero => item => getState(item) > 0,
                FollowPredicate.IsLessThanOrEqualToZero => item => getState(item) <= 0,
                FollowPredicate.IsGreaterThanOrEqualToZero => item => getState(item) >= 0,
                FollowPredicate.IsTrue => item => getState(item) != 0,
                FollowPredicate.IsFalse => item => getState(item) == 0,
                _ => throw new NotSupportedException(
                    $"Unsupported {nameof(FollowPredicate)}: {Condition}")
            };
        }



        FollowPredicate _condition = (FollowPredicate)(-1);
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
        internal void UpdateCurrentItemsArray() => OnPropertyChanged(nameof(CurrentItems));

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ModifiersRequestEventArgs>? ModifiersRequest;

        WatchdogTimer WDTSettle
        {
            get
            {
                if (_wdtSettle is null)
                {
                    _wdtSettle = new WatchdogTimer { Interval = TimeSpan.FromSeconds(0.1) };
                }
                _wdtSettle.RanToCompletion += (sender, e) =>
                {
                    OnPropertyChanged(nameof(CurrentItems));
                };
                return _wdtSettle;
            }
        }

        internal FollowValueDomain FollowValueDomain { get; }

        WatchdogTimer? _wdtSettle = null;
    }
}