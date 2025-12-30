using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Threading;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections.Lists
{
    [DebuggerDisplay("Count={base.Count}")]
    public partial class ObservablePreviewCollection<T> : IObservablePreviewCollection<T>
    {
        public ObservablePreviewCollection()
        {
            // Please do not remove.
            Throw.BeginThrowOrAdvise += (sender, e) =>
            {
                switch (e.Mode)
                {
                    case ThrowOrAdvise.ThrowFramework:
                        { } // <- Pause here and L O O K
#if DEBUG
                        // This may seem backwards, but in fact, we want
                        // it soft during debugging but in production
                        // the EUD needs to know if the framework errs.
                        e.Handled = true;
#endif
                        break;
                }
            };
            InitializeTrackContexts();
        }

        public ObservablePreviewCollection(IEnumerable<T> items) 
            : this()
        {
            // There isn't going to be any eventing set
            // up yet, so perform this without ceremony.
            var ePre = new NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction.Add, items);
            OnCollectionChanging(ePre);
            Distinctifier.SyncReset();
        }

        public new T this[int index]
        {
            get
            {
                if(index > Count)
                {
                    // Inrecoverable
                    this.ThrowHard<IndexOutOfRangeException>();
                    return default!;
                }
                if (index == Count)
                {
                    switch (Mode)
                    {
                        case ListMode.Normal:
                            this.ThrowHard<IndexOutOfRangeException>();
                            break;
                        case ListMode.TolerantReturnDefault:
                        case ListMode.TolerantCreateDefaultEntry:
                        case ListMode.InsistentNotNull:
                            // [Canonical]
                            // Signature of a remedial pre event is null:null.
                            var ePre = new NotifyCollectionChangingEventArgs(
                                action: NotifyCollectionChangingAction.Replace,
                                oldItem: null,
                                newItem: null);

                            // Do *not* go through OnCollectionChanging method.
                            CollectionChanging?.Invoke(this, ePre);

                            return ePre.GetNewItemSingle<T>()!;
                        default:
                            this.ThrowHard<NotSupportedException>($"The {Mode.ToFullKey()} case is not supported.");
                            break;
                    }
                    return default!;
                }
                else
                {
                    return base[index];
                }
            }
            set
            {
                NotifyCollectionChangingEventArgs ePre;
                if(index == -1)
                {
                    this.ThrowHard<IndexOutOfRangeException>();
                    return;
                }
                else if (index < Count)
                {
                    ePre = new(
                        action: NotifyCollectionChangingAction.Replace,
                        newItem: value,
                        oldItem: base[index],
                        index: index);
                }
                else if (index == Count)
                {
                    ePre = new(
                        action: NotifyCollectionChangingAction.Add,
                        changedItem: value,
                        index);
                }
                else
                {
                    // Do *not* preview this (i.e. as a correctable fault).
                    // This is an early warning system of a fundamentally flawed proposal.
                    this.ThrowHard<IndexOutOfRangeException>();
                    return;
                }
                OnCollectionChanging(ePre);
            }
        }

        public bool IsReadOnly => false;

        public ListMode Mode { get; set; } = ListMode.Normal;

        public new void Add(T? item)
        {
            NotifyCollectionChangingEventArgs ePre = new (
                action: NotifyCollectionChangingAction.Add,
                changedItem: item);
            OnCollectionChanging(ePre);
        }

        public new void Clear()
        {
            NotifyCollectionChangingEventArgs ePre = new (
                action: NotifyCollectionChangingAction.Reset,
                changedItems: this.ToArray());
            OnCollectionChanging(ePre);
        }

        public new bool Contains(T item)
        {
            if (_optimizationMode.HasFlag(ListOptimizationMode.UseCacheForContains))
            {
                return Distinctifier.Contains(item);
            }
            else
            {
                return base.Contains(item);
            }
        }

        public new int IndexOf(T item)
        {
            return base.IndexOf(item);
        }

        public new void Insert(int index, T item)
        {
            if (index == -1 || index > Count)
            {
                this.ThrowHard<IndexOutOfRangeException>();
            }
            else
            {
                NotifyCollectionChangingEventArgs ePre = new(
                    action: NotifyCollectionChangingAction.Add,
                    changedItem: item,
                    index: index);
                OnCollectionChanging(ePre);
            }
        }

        public new bool Remove(T item)
        {
            NotifyCollectionChangingEventArgs ePre = new(
                action: NotifyCollectionChangingAction.Remove,
                changedItem: item);
            OnCollectionChanging(ePre);
            return ePre.GetAppliedChangesCount() == 1;
        }

        public new void RemoveAt(int index)
        {
            if (index == -1 || index >= Count)
            {
                // Do *not* preview this (i.e. as a correctable fault).
                // This is an early warning system of a fundamentally flawed proposal.
                this.ThrowHard<IndexOutOfRangeException>();
            }
            else
            {
                NotifyCollectionChangingEventArgs ePre = new(
                    action: NotifyCollectionChangingAction.Remove,
                    changedItem: base[index],
                    index: index);
                OnCollectionChanging(ePre);
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        protected virtual void OnCollectionChanging(NotifyCollectionChangingEventArgs e)
        {
            e.OnAwaited();
            CollectionChanging?.Invoke(this, e);
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
        public event NotifyCollectionChangingEventHandler? CollectionChanging;

        protected T[] PreChangeSnapshot { get; set; } = [];
        protected virtual void ApplyChanges(NotifyCollectionChangingEventArgs e)
        {
            if (ActiveFilters.Any())
            {
                PreChangeSnapshot = this.ToArray();
            }
            e.OnAwaited();
            // Look for special DTOs
            if (e.NewItems?.Count == 1)
            {
                switch (e.NewItems[0])
                {
                    case CollectionRange range:
                        ApplyRanges(e);
                        return;
                    default:
                        break;
                }
            }
            if (e.OldItems?.Count == 1)
            {
                switch (e.OldItems[0])
                {
                    case CollectionRange range:
                        ApplyRanges(e);
                        return;
                    default:
                        break;
                }
            }

            int countApplied = 0;
            var action = e.Action.ToBCLAction().AsEnumType<NotifyCollectionChangingAction>();
            object? errorItem;

            // Does not have to succeed. Legitimately screens to make
            // sure a non-T value hasn't been coreced in the handler.
            _ = TryGetSafeBuffer(e.NewItems, out IList<T> newItemsT, out errorItem);
            if (errorItem is not null)
            {
                e.ThrowHard<InvalidCastException>($"The preview event contains an incompatible coerced new value.");
                return;
            }
            _ = TryGetSafeBuffer(e.OldItems, out IList<T> oldItemsT, out errorItem);
            if (errorItem is not null)
            {
                e.ThrowHard<InvalidCastException>($"The preview event contains an incompatible coerced old value.");
                return;
            }

            var newItems = newItemsT?.ToArray() ?? [];
            var oldItems = oldItemsT?.ToArray() ?? [];
            switch (action)
            {
                case NotifyCollectionChangingAction.Add:
                    if (e.NewStartingIndex == -1)
                    {
                        // NOTES:
                        // - Distinctifier carries its own lock.
                        // - There is an unavoidable transient mismatch of sanity count with 
                        //  list count. What the transaction does is defer the parity check.
                        foreach (var itemT in newItemsT ?? [])
                        {
                            using (Distinctifier.BeginAtomic())
                            {
                                Distinctifier.Add(itemT);
                                base.Add(itemT);
                                countApplied++;
                            }
                        }
                    }
                    else
                    {
                        // Execute an INSERT loop.
                        if (e.NewStartingIndex <= Count)
                        {
                            var currentIndex = e.NewStartingIndex;
                            foreach (var itemT in newItemsT ?? [])
                            {
                                using (Distinctifier.BeginAtomic())
                                {
                                    Distinctifier.Add(itemT);
                                    base.Insert(currentIndex++, itemT);
                                    countApplied++;
                                }
                            }
                        }
                        else
                        {
                            this.ThrowHard<IndexOutOfRangeException>();
                            return;
                        }
                    }
                    break;

                case NotifyCollectionChangingAction.Remove:
                    if (e.OldItems?.OfType<CollectionRange>().FirstOrDefault() is { } range)
                    {
                        this.ThrowFramework<InvalidOperationException>("Should have been handled as a DTO.");
                        return;
                    }
                    else
                    {
                        if (e.OldStartingIndex == -1)
                        {
                            foreach (var item in oldItemsT ?? [])
                            {
                                bool removeD, removeB;
                                using (Distinctifier.BeginAtomic())
                                {
                                    removeD = Distinctifier.Remove(item);
                                    // Not an else
                                    removeB = base.Remove(item);
                                }
                                if (removeD ^ removeB)
                                {
                                    // If EUD has any issues in optimized mode, make sure that
                                    // turning off optimizations makes the exceptions go away too.
                                    if (OptimizationMode == ListOptimizationMode.UseCacheForContains)
                                    {
                                        this.ThrowFramework<InvalidOperationException>($"Expecting both or neither.");
                                    }
                                }
                                else
                                {
                                    if (removeB) // Then they both are.
                                    {
                                        countApplied++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (e.NewItems is not null || e.NewStartingIndex != -1 || e.OldItems?.Count != 1)
                            {
                                e.ThrowFramework<InvalidOperationException>($"Illegal parameters for {nameof(base.RemoveAt)}");
                                return;
                            }
                            if (e.OldStartingIndex >= Count)
                            {
                                e.ThrowHard<IndexOutOfRangeException>(
                                    $"Illegal parameters for {nameof(base.RemoveAt)}");
                                return;
                            }
                            using (Distinctifier.BeginAtomic())
                            {
                                Distinctifier.Remove(e.OldItems![0]);
                                base.RemoveAt(e.OldStartingIndex);
                                countApplied++;
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangingAction.Replace:
                    if (oldItemsT?.Count == 1 && newItemsT?.Count == 1)
                    {
                        var oldItem = oldItemsT[0];
                        var newItem = newItemsT[0];
                        if (e.OldStartingIndex != e.NewStartingIndex)
                        {
                            e.ThrowHard<InvalidOperationException>(
                                $"The event signature for [Indexer] Replace requires " +
                                $"{nameof(e.OldStartingIndex)}=={nameof(e.NewStartingIndex)}");
                            return;
                        }
                        var index = e.NewStartingIndex;
                        if (index == -1) // Still...
                        {
                            e.ThrowHard<InvalidOperationException>(
                                "The item being replaced is no longer present in the collection.");
                            return;
                        }
                        if (index > Count)
                        {
                            e.ThrowHard<IndexOutOfRangeException>(
                                $"Legal range for [Indexer] set is 0 to {Count}");
                            return;
                        }

                        using (Distinctifier.BeginAtomic())
                        {
                            // Replace is logically Remove(old) + Add(new)
                            Distinctifier.Remove(oldItem);
                            Distinctifier.Add(newItem);
                            base[index] = newItem;
                            countApplied++;
                        }
                    }
                    else
                    {
                        e.ThrowHard<NotSupportedException>(
                            "Replace must provide exactly one old item and one new item.");
                        return;
                    }
                    break;

                /// <summary>
                /// Ensures that a Move operation targets an existing span of index
                /// territory in the pre-mutation list.
                /// </summary>
                /// <remarks>
                /// A Move repositions an existing block; it does not create new
                /// index territory. The target index must fall within the current
                /// index domain, and a multi-item block must fit entirely within it.
                /// 
                /// Invariant:
                ///     newIndex + blockLength <= Count
                /// 
                /// Examples (Count = 10):
                ///     Move 8 -> 9           legal      (length = 1)
                ///     Move 8 -> 10          illegal    (index 10 does not exist)
                ///     Move [8,9] -> 9       illegal    (block would extend past end)
                ///     Move [7,8] -> 9       legal      (block fits before end)
                /// 
                /// This preserves Move semantics as strict repositioning within the
                /// current index graph. Requests that imply insertion are rejected.
                /// </remarks>
                case NotifyCollectionChangingAction.Move:
                    var count = oldItems.Length;
                    var oldIndex = e.OldStartingIndex;
                    var newIndex = e.NewStartingIndex;

                    // Validate newIndex against pre-mutation index territory.

                    if ((oldIndex < 0 || oldIndex >= Count))
                    {
                        e.ThrowHard<IndexOutOfRangeException>(
                            $"Legal range for {nameof(e.OldStartingIndex)} is 0 to {Count - 1}");
                        return;
                    }

                    var newIndexMax = newIndex + oldItems.Length;
                    if (newIndex < 0 || newIndexMax > Count)
                    {
                        var message =
                            oldItems.Length < 2
                            ? $"Legal range for {nameof(e.NewStartingIndex)} is 0 to {Count}"
                            : $"Legal range (allowing for {count} items) is 0 to {(Count - newItems.Length) - 1}";
                        e.ThrowHard<IndexOutOfRangeException>(message);
                        return;
                    }

                    if (count == 0)
                    {
                        // Nothing to move.
                        return;
                    }
                    else
                    {
                        // Extract the contiguous block.
                        var items = new List<T>();
                        for (int i = 0; i < count; i++)
                        {
                            items.Add(this[oldIndex]);
                            base.RemoveAt(oldIndex);
                        }

                        // Reinsert sequentially, respecting the validated target start.
                        for (int i = 0; i < count; i++, newIndex++)
                        {
                            int insertOrAppendIndex = Math.Min(newIndex, Count);
                            base.Insert(insertOrAppendIndex, items[i]);
                        }
                        countApplied += count;
                    }
                    break;


                case NotifyCollectionChangingAction.Reset:
                    using (Distinctifier.BeginAtomic())
                    {
                        Distinctifier.Clear();
                        base.Clear(); // EUD has now had the opportunity to dispose resources.
                    }
                    break;
                default:
                    this.ThrowHard<NotSupportedException>($"The {action.ToFullKey()} case is not supported.");
                    break;
            }
            e.SetAppliedChangesCount(countApplied);

            if (OptimizationMode.HasFlag(ListOptimizationMode.TrackItemPropertyChanges))
            {
                ManageItemSubscriptions(e);
            }
            OnCollectionChanged(e.CopyToChangedEvent());
        }

        protected virtual void ApplyRanges(NotifyCollectionChangingEventArgs e)
        {
            CollectionRange? range = null;
            switch (e.Action)
            {
                case NotifyCollectionChangingAction.Add:
                    if (e.NewItems?.Count == 1)
                    {
                        range = e.NewItems[0] as CollectionRange;
                    }
                    if (range is null)
                    {
                        this.ThrowFramework<NullReferenceException>();
                    }
                    else
                    {
                        Debug.Fail($@"ADVISORY - First Time.");
                    }
                    break;
                case NotifyCollectionChangingAction.Remove:
                    if(e.OldItems?.Count == 1)
                    {
                        range = e.OldItems[0] as CollectionRange;
                    }
                    if (range is null)
                    {
                        this.ThrowFramework<NullReferenceException>();
                    }
                    else
                    {
                        if (range.StartIndex == -1
                            || range.StartIndex >= Count 
                            || range.EndIndex == -1 
                            || range.EndIndex >= Count)
                        {
                            this.ThrowHard<IndexOutOfRangeException>($"{nameof(CollectionRange)} object contains an out-of-range index.");
                            return;
                        }
                        else
                        {
                            for (int i = 0; i < range.Count; i++)
                            {
                                using (Distinctifier.BeginAtomic())
                                {
                                    var remove = base[range.StartIndex];
                                    Distinctifier.Remove(remove);
                                    base.RemoveAt(range.StartIndex);
                                }
                            }
                        }
                    }
                    break;
                default:
                    this.ThrowFramework<InvalidOperationException>(
                        $"Bad case for Range: {e.Action.ToFullKey()}");
                    return;
            }
            OnCollectionChanged(e.CopyToChangedEvent());
        }

        private void ManageItemSubscriptions(NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangingAction.Add:
                    localSubscribe(e.NewItems?.OfType<T>());
                    break;

                case NotifyCollectionChangingAction.Remove:
                    localUnsubscribe(e.OldItems?.OfType<T>());
                    break;

                case NotifyCollectionChangingAction.Replace:
                    localUnsubscribe(e.OldItems?.OfType<T>());
                    localSubscribe(e.NewItems?.OfType<T>());
                    break;

                case NotifyCollectionChangingAction.Reset:
                    // Everything is about to be removed.
                    localUnsubscribe(e.OldItems?.OfType<T>());
                    break;

                case NotifyCollectionChangingAction.Move:
                    // Structural only; no membership change.
                    break;
            }
            #region L o c a l F x 
            void localSubscribe(IEnumerable<T>? items)
            {
                if (items is null) return;
                foreach (var item in items)
                {
                    if (item is INotifyPropertyChanging inpcPre)
                    {
                        inpcPre.PropertyChanging-= OnItemPropertyChanging;
                        inpcPre.PropertyChanging += OnItemPropertyChanging;
                    }
                    if (item is INotifyPropertyChanged inpcPost)
                    {
                        inpcPost.PropertyChanged -= OnItemPropertyChanged;
                        inpcPost.PropertyChanged += OnItemPropertyChanged;
                    }
                }
            }

            void localUnsubscribe(IEnumerable<T>? items)
            {
                if (items is null) return;
                foreach (var item in items)
                {
                    if (item is INotifyPropertyChanged inpc)
                    {
                        inpc.PropertyChanged -= OnItemPropertyChanged;
                    }
                }
            }
            #endregion L o c a l F x
        }

        /// <summary>
        /// Provides a mechanism for O(1) Contains.
        /// </summary>
        Distinctifier Distinctifier
        {
            get
            {
                if (_distinctifier is null)
                {
                    _distinctifier = new Distinctifier(this);
                }
                return _distinctifier;
            }
        }
        Distinctifier? _distinctifier = null;

        public ListOptimizationMode OptimizationMode
        {
            get => _optimizationMode;
            set
            {
                if (!Equals(_optimizationMode, value))
                {
                    _optimizationMode = value;
                    TrackItemPropertyChanges = _optimizationMode.HasFlag(ListOptimizationMode.TrackItemPropertyChanges);
                    OnPropertyChanged();
                }
                if(_optimizationMode.HasFlag(ListOptimizationMode.UseCacheForContains))
                {
                    // The flag doesn't need to *change* for this
                    // to happen. It just needs to be present.
                    Distinctifier.SyncReset();
                }
            }
        }
        ListOptimizationMode _optimizationMode = ListOptimizationMode.Normal;

        protected bool TrackItemPropertyChanges
        {
            get => _trackItemChanges;
            set
            {
                if (!Equals(_trackItemChanges, value))
                {
                    if (_trackItemChanges)
                    {
                        if (IsFiltering)
                        {
                            foreach(var inpc in UnfilteredItems.OfType<INotifyPropertyChanged>())
                            {
                                inpc.PropertyChanged -= OnItemPropertyChanged;
                            }
                        }
                        else
                        {
                            foreach(var inpc in this.OfType<INotifyPropertyChanged>())
                            {
                                inpc.PropertyChanged -= OnItemPropertyChanged;
                            }
                        }
                    }
                    _trackItemChanges = value;
                    if (_trackItemChanges)
                    {
                        if (IsFiltering)
                        {
                            foreach (var inpc in UnfilteredItems.OfType<INotifyPropertyChanged>())
                            {
                                inpc.PropertyChanged += OnItemPropertyChanged;
                            }
                        }
                        else
                        {
                            foreach (var inpc in this.OfType<INotifyPropertyChanged>())
                            {
                                inpc.PropertyChanged += OnItemPropertyChanged;
                            }
                        }
                    }
                    OnPropertyChanged();
                }
            }
        }
        bool _trackItemChanges = false;

        public bool AddDistinct(T item)
        {
            if(Contains(item))
            {
                return false;
            }
            NotifyCollectionChangingEventArgs ePre = new(
                action: NotifyCollectionChangingAction.Add,
                changedItem: item);
            OnCollectionChanging(ePre);

            return ePre.GetAppliedChangesCount() != 0;
        }
    }
}
