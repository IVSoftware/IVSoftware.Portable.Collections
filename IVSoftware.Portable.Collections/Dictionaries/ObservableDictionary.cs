using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace IVSoftware.Portable.Collections.Dictionaries;

/// <summary>
/// Provides a dictionary that raises pre- and post-change notifications for all mutations.
/// </summary>
/// <remarks>
/// Serves as the base personality for tolerant, insistent, and brisk variants.
/// Event sequencing mirrors <see cref="INotifyCollectionChanged"/> but includes
/// cancellable pre-events for finer control.
/// </remarks>
public partial class ObservableDictionary<TKey, TValue>
    : IDictionary
    , IDictionary<TKey, TValue?>
    , IObservableDictionary<TKey, TValue?>
    , IUpgradeableDictionary
    where TKey : notnull
{
    /// <summary>
    /// PROTECTED: Subclasses will need direct access to the
    /// setter for when their getters modify the collection.
    /// </summary>
    protected readonly Dictionary<TKey, TValue?> @base = new();

    [Indexer]
    public virtual TValue? this[TKey key]
    {
        get => @base[key];
        set
        {
            NotifyCollectionChangingEventArgs ePre;
            if (@base.TryGetValue(key, out var oldValue))
            {
                ePre = new NotifyCollectionChangingEventArgs(
                    action: NotifyCollectionChangingAction.Replace,
                    newItem: new DictionaryEntryPreview(key, value),
                    oldItem: new DictionaryEntryPreview(key, oldValue));
            }
            else
            {
                ePre = new NotifyCollectionChangingEventArgs(
                    action: NotifyCollectionChangingAction.Add,
                    changedItem: new DictionaryEntryPreview(key, value));
            }
            OnCollectionChanging(ePre);
            if (ePre.Cancel)
            {
                this.ThrowSoft<OperationCanceledException>();
            }
        }
    }
    public ICollection<TKey> Keys => @base.Keys;
    public ICollection<TValue?> Values => @base.Values;
    public int Count => @base.Count;
    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)@base).IsReadOnly;
    public virtual void Add(TKey key, TValue? value)
    {
        if(ContainsKey(key))
        {
            this.ThrowHard<ArgumentException>(
    $"An element with the same key ('{key}') already exists in the dictionary."
);
            return;
        }
        this[key] = value;
    }
    public void Add(KeyValuePair<TKey, TValue?> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)this).Add(item);
    public virtual void AddRange(IEnumerable<DictionaryEntryPreview> entries)
    {
        var e = new NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction.Add, changedItems: entries.ToList());
        OnCollectionChanging(e);
    }
    public virtual void Clear()
    {
        var ePre = new NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction.Reset);
        OnCollectionChanging(ePre);
    }
    public bool Contains(KeyValuePair<TKey, TValue?> item) => ((ICollection<KeyValuePair<TKey, TValue?>>)@base).Contains(item);
    public bool ContainsKey(TKey key) => @base.ContainsKey(key);
    public void CopyTo(KeyValuePair<TKey, TValue?>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue?>>)@base).CopyTo(array, arrayIndex);
    public IEnumerator<KeyValuePair<TKey, TValue?>> GetEnumerator() => @base.GetEnumerator();
    public virtual bool Remove(TKey key)
    {
        if (@base.TryGetValue(key, out var oldValue))
        {
            var ePre = new NotifyCollectionChangingEventArgs(
                action: NotifyCollectionChangingAction.Remove,
                changedItem: new DictionaryEntryPreview(key, oldValue));
            OnCollectionChanging(ePre);
            if (ePre.Cancel)
            {
                this.ThrowSoft<OperationCanceledException>();
            }
            return ePre.GetAppliedChangesCount() == 1;
        }
        else
        {
            return false;
        }
    }
    public virtual bool Remove(KeyValuePair<TKey, TValue?> item)
    {
        return Remove(item.Key);
    }

    public bool TryGetValue(TKey key, out TValue? value)
        => @base.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => @base.GetEnumerator();


    /// <summary>
    /// Subclasses must limit the allowed values.
    /// </summary>
    public virtual DictionaryMode Mode
    {
        get => DHostEphemeralMode.IsZero() ? _constrainedMode : EphemeralMode;
        protected set
        {
            if (!Equals(_constrainedMode, value))
            {
                _constrainedMode = value;
                OnPropertyChanged();
            }
        }
    }
    DictionaryMode _constrainedMode = DictionaryMode.Normal;
    protected DictionaryMode EphemeralMode { get; private set; } = DictionaryMode.Normal;


    /// <summary>
    /// Modes other than Insistent may only be checked out for the scope of a block.
    /// </summary>
    /// <remarks>
    /// Proper 'using' hygiene is required.
    /// </remarks>
    public DisposableHost DHostEphemeralMode
    {
        get
        {
            if (_dhostMode is null)
            {
                _dhostMode = new DisposableHost();
                _dhostMode.CountChanged += (sender, e) =>
                {
                    if (e.Token.Sender is DictionaryMode ephemeralMode)
                    {
                        switch (e.Action)
                        {
                            case CountChangedAction.Push:
                                EphemeralMode = ephemeralMode;
                                break;
                            case CountChangedAction.Pop:
                                if(_dhostMode.Tokens.LastOrDefault() is { } token)
                                {
                                    if(token.Sender is DictionaryMode ephemeralModePrev)
                                    {
                                        EphemeralMode = ephemeralModePrev;
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        this.ThrowHard<InvalidOperationException>("Make sure to set a DictionaryMode as the sender.");
                    }
                };
            }
            return _dhostMode;
        }
    }
    DisposableHost? _dhostMode = null;

    [Canonical("Three State CollectionChanging Handler")]
    protected virtual void OnCollectionChanging(NotifyCollectionChangingEventArgs e)
    {
        _collectionChanging?.Invoke(this, e);
        Framework.RaiseEvent(this, e);

        if (e.Cancel)
        {
            this.ThrowSoft<OperationCanceledException>();
        }
        else
        {
            ApplyChanges(e);
        }
    }

    /// <summary>
    /// Subclasses can explicitly raise the CollectionChanging 
    /// event and skip the default ApplyChanges phase.
    /// </summary>
    protected void RaiseCollectionChanging(NotifyCollectionChangingEventArgs e)
    {
        _collectionChanging?.Invoke(this, e);
    }

    protected virtual void ApplyChanges(NotifyCollectionChangingEventArgs e)
    {
        var action = e.Action.ToBCLAction().AsEnumType<NotifyCollectionChangingAction>();
        var countApplied = 0;
        switch (action)
        {
            case NotifyCollectionChangingAction.Add:
                foreach (var entry in e.NewItems?.OfType<DictionaryEntryPreview>() ?? [])
                {
                    if (entry.Key is TKey keyT && entry.Value.IsAssignableAs(out TValue? valueT))
                    {
                        @base[keyT] = valueT;
                        countApplied++;
                    }
                    else
                    {
                        this.ThrowHard<InvalidCastException>();
                    }
                }
                break;
            case NotifyCollectionChangingAction.Remove:
                foreach (var entry in e.OldItems?.OfType<DictionaryEntryPreview>() ?? [])
                {
                    if (entry.Key is TKey keyT && @base.ContainsKey(keyT))
                    {
                        @base.Remove(keyT);
                        countApplied++;
                    }
                    else
                    {
                        this.ThrowHard<InvalidCastException>();
                    }
                }
                break;
            case NotifyCollectionChangingAction.Replace:

                // Detector for calls from [Indexer] get for behaviors like Insistent and Tolerant.
                bool isBehavioralGET = false;

                // REMOVE old dictionary entries.
                foreach (var entry in e.OldItems?.OfType<DictionaryEntryPreview>() ?? [])
                {
                    // Canonical signature
                    isBehavioralGET = entry.Key is null;

                    if (isBehavioralGET)
                    {
                        // Key is null. Nothing to remove. Crash if you try.
                        switch (Mode)
                        {
                            case DictionaryMode.Normal:
                                Debug.Fail($"The {nameof(entry.Key)} is only allowed to be null in the getter [Indexer] of modal dictionaries.");
                                break;
                            case DictionaryMode.TolerantReturnDefault:
                            case DictionaryMode.TolerantCreateDefaultEntry:
                            case DictionaryMode.InsistentNotNull:
                            case DictionaryMode.Brisk:
                                break;
                            default:
                                this.ThrowHard<NotSupportedException>($"The {Mode.ToFullKey()} case is not supported.");
                                return;
                        }
                    }
                    else
                    {
                        if (entry.Key is TKey keyT)
                        {
                            if (@base.ContainsKey(keyT))
                            {
                                @base.Remove(keyT);
                                countApplied++;
                            }
                        }
                        else
                        {
                            this.ThrowHard<InvalidCastException>();
                        }
                    }
                }

                // ADD or Insert at new key.
                foreach (var entry in e.NewItems?.OfType<DictionaryEntryPreview>() ?? [])
                {
                    if (entry.Key is TKey keyT)
                    {
                        if (entry.Value is TValue valueT)
                        {
                            @base[keyT] = valueT;
                            countApplied++;
                        }
                        else if (isBehavioralGET)
                        {
                            // Check for explicit null
                            if (Equals(entry.Value, TolerantValue.ExplicitNull))
                            {
                                @base[keyT] = default;
                            }
                            else
                            {   /* G T K */
                                // i.e a tolerant getter that has taken no action.
                            }
                        }
                    }
                    else
                    {
                        this.ThrowHard<InvalidCastException>();
                    }
                }
                break;
            case NotifyCollectionChangingAction.Move:
                this.ThrowHard<NotSupportedException>();
                return;
            case NotifyCollectionChangingAction.Reset:
                countApplied = Count;
                @base.Clear();
                break;
            default:
                this.ThrowFramework<NotSupportedException>($"The {action.ToFullKey()} case is not supported.", @throw: false);
                return;
        }
        e.SetAppliedChangesCount(countApplied);
        OnCollectionChanged(e.CopyToChangedEvent());
    }

    /// <summary>
    /// Maintains a list of subscriptions so that dict
    /// upgrades  can transfer handlers over intact.
    /// </summary>
    public event NotifyCollectionChangingEventHandler? CollectionChanging
    {
        add
        {
            if(value is not null)
            {
                _collectionChanging += value;
                _changingHandlers.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                _collectionChanging -= value;
                _changingHandlers.Remove(value);
            }
        }
    }
    private event NotifyCollectionChangingEventHandler? _collectionChanging;
    private List<NotifyCollectionChangingEventHandler> _changingHandlers = new();

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        _collectionChanged?.Invoke(this, e);
        Framework.RaiseEvent(this, e);
    }

    /// <summary>
    /// Maintains a list of subscriptions so that dict
    /// upgrades  can transfer handlers over intact.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add
        {
            if (value is not null)
            {
                _collectionChanged += value;
                _changedHandlers.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                _collectionChanged -= value;
                _changedHandlers.Remove(value);
            }
        }
    }
    private event NotifyCollectionChangedEventHandler? _collectionChanged;
    private List<NotifyCollectionChangedEventHandler> _changedHandlers = new();


    (int countChanging, int countChanged) IUpgradeableDictionary.TransferEvents(IObservableDictionary upgrade)
        => TransferEvents(upgrade);
    internal (int countChanging, int countChanged) TransferEvents(IObservableDictionary upgrade)
    {
        int 
            countChangingSuccess = 0,
            countChangedSuccess = 0;
        if (upgrade is null)
        {
            this.ThrowHard<ArgumentNullException>(nameof(upgrade));
        }
        else
        {
            // Transfer CollectionChanging handlers.
            if (_changingHandlers.Count > 0)
            {
                foreach (var onCollectionChanging in _changingHandlers.ToArray())
                {
                    upgrade.CollectionChanging += onCollectionChanging;
                    this.CollectionChanging -= onCollectionChanging;
                    countChangingSuccess++;
                }
            }

            // Transfer CollectionChanged handlers.
            if (_changedHandlers.Count > 0)
            {
                foreach (var onCollectionChanged in _changedHandlers.ToArray())
                {
                    upgrade.CollectionChanged += onCollectionChanged;
                    this.CollectionChanged -= onCollectionChanged;
                    countChangedSuccess++;
                }
            }
        }
        return (countChangingSuccess, countChangedSuccess);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// The only real reason for having this (so far) is to forward Batchable property changes.
    /// </summary>
    protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ReferenceEquals(sender, this))
        {
            PropertyChanged?.Invoke(sender, e);
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
