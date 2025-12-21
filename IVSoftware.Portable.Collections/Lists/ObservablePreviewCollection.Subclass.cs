using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Threading;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.IO.Pipes;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> : ObservableCollection<T>, ISuppressibleEventSource
    {
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.OnAwaited();

            if (DHostSuppress.IsZero()) // Public, preeminent suppression.
            {
                // UPGRADE 251126
                if (e is NotifyPreviewCollectionChangedEventArgs)
                {
                    if(MarkdownContext?.IsFiltering == true && DHostSuspendTracking.IsZero())
                    {
                        TrackVisibleCollectionChanges(e);
                    }
                    // Now raise the 'master' event stating everything is synced as we know it.
                    base.OnCollectionChanged(e);
                    Framework.RaiseEvent(this, e); // Static version
                }
                else
                {
                    // N O O P
                    // Ignore any events that we didn't construct ourselves.
                }
            }
            else
            {
                EventSuppressed?.Invoke(this, e);
            }
        }

        private void TrackVisibleCollectionChanges(NotifyCollectionChangedEventArgs e)
        {
            if (FollowContexts.Any())
            {
                bool anyError = false;
                var indexMap = CreateIndexMap();
                int? u; // Unfiltered index
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        int newStartingIndex;
                        if (e.NewStartingIndex != -1 && e.NewStartingIndex < PreChangeSnapshot.Length)
                        {
                            newStartingIndex = indexMap[e.NewStartingIndex];
                        }
                        else
                        {
                            newStartingIndex = PreChangeSnapshot.Length; // Visible list count is the authority.
                        }
                        foreach (T item in e.NewItems ?? new T[0])
                        {
                            ItemsSourceProtected.Insert(newStartingIndex++, item);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems is not null && e.OldItems.Count > 0)
                        {
                            if (e.OldItems[0] is CollectionRange range)
                            {
                                var rangeIndexes = new List<int>();
                                for (u = range.EndIndex; u >= range.StartIndex; u--)
                                {
                                    rangeIndexes.Add(indexMap[(int)u]);
                                }
                                foreach (int i in rangeIndexes)
                                {
                                    ItemsSourceProtected.RemoveAt(i);
                                }
                            }
                            else
                            {
                                if (TryGetPrimaryKeyProperty(out var pi))
                                {
                                    // Remove by object
                                    foreach (T item in e.OldItems)
                                    {
                                        for (int uu = 0; uu < ItemsSourceProtected.Count; uu++)
                                        {
                                            if (pi.GetValue(item)?.ToString() is { } id &&
                                                id == pi.GetValue(ItemsSourceProtected[uu])?.ToString())
                                            {
                                                ItemsSourceProtected.RemoveAt(uu);
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Remove by object
                                    foreach (T item in e.OldItems)
                                    {
                                        ItemsSourceProtected.Remove(item);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Remove by index
                            if (e.OldStartingIndex is -1 || e.OldStartingIndex >= PreChangeSnapshot.Length)
                            {
                                // Pre-translation error
                                this.ThrowHard<IndexOutOfRangeException>(
                                    $"Cannot remove item at index={e.OldStartingIndex}");
                                anyError = true;
                            }
                            else
                            {
                                u = indexMap[e.OldStartingIndex];
                                if (u is null || u is -1 || u >= ItemsSourceProtected.Count)
                                {
                                    // Post-translation error
                                    this.ThrowHard<IndexOutOfRangeException>(
                                        $"Cannot remove item at translated index={u}");
                                    anyError = true;
                                }
                                else
                                {
                                    ItemsSourceProtected.RemoveAt((int)u);
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldStartingIndex < 0 ||
                            e.OldStartingIndex >= PreChangeSnapshot.Length)
                        {
                            this.ThrowHard<IndexOutOfRangeException>(
                                $"Cannot replace item at index={e.OldStartingIndex}");
                            anyError = true;
                        }
                        u = indexMap[e.OldStartingIndex];
                        if (u is null || u is -1 || u >= ItemsSourceProtected.Count)
                        {
                            // Post-translation error
                            this.ThrowHard<IndexOutOfRangeException>(
                                $"Cannot remove item at translated index={u}");
                            anyError = true;
                        }
                        else
                        {
                            if (e.NewItems is null || e.NewItems.Count == 0)
                            {
                                this.ThrowHard<InvalidOperationException>(
                                    "Replace action new item is missing.");
                                anyError = true;
                            }
                            else if (e.NewItems[0] is not T)
                            {
                                this.ThrowHard<InvalidOperationException>(
                                    $"Replace action contains item of unexpected type {e.NewItems[0]?.GetType().Name}.");
                                anyError = true;
                            }
                            else
                            {
                                ItemsSourceProtected[(int)u] = (T)e.NewItems[0]!;
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        // Validate visible indices (pre-change space)
                        if (e.OldStartingIndex < 0 ||
                            e.OldStartingIndex >= PreChangeSnapshot.Length ||
                            e.NewStartingIndex < 0 ||
                            e.NewStartingIndex >= PreChangeSnapshot.Length)
                        {
                            this.ThrowHard<IndexOutOfRangeException>(
                                $"Cannot move item from {e.OldStartingIndex} to {e.NewStartingIndex}");
                            anyError = true;
                            break;
                        }

                        int? from = indexMap[e.OldStartingIndex];
                        int? to = indexMap[e.NewStartingIndex];

                        if (from is null || from is -1 ||
                            to is null || to is -1 ||
                            from >= ItemsSourceProtected.Count ||
                            to >= ItemsSourceProtected.Count)
                        {
                            this.ThrowHard<InvalidOperationException>(
                                "Index mapping failed. Visible and canonical collections are out of sync.");
                            anyError = true;
                            break;
                        }
                        // Extract the item
                        var move = ItemsSourceProtected[(int)from];
                        ItemsSourceProtected.RemoveAt((int)from);
                        // If the item was removed before the insertion point,
                        // the target index shifts left by one
                        if (from < to)
                        {
                            to--;
                        }
                        ItemsSourceProtected.Insert((int)to, move);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        ItemsSourceProtected.Clear();
                        break;
                    default:
                        this.ThrowHard<NotSupportedException>($"The {e.Action.ToFullKey()} case is not supported.");
                        break;
                }
                if (anyError)
                {
                    // Incremental sync failed. Rebuild visible collection from canonical truth.
                    using (DHostSuspendTracking.GetToken())
                    using (DHostSuppress.GetToken())
                    {
                        Clear();
                        ClearFilters();
                        foreach (T item in ItemsSourceProtected)
                        {
                            Add(item);
                        }
                    }
                }
            }
        }

        protected virtual TolerantDictionary<int, int> CreateIndexMap()
        {
            var indexMap = new TolerantDictionary<int, int>();
            int u = 0;
            for (int i = 0; i < PreChangeSnapshot.Length && u < ItemsSourceProtected.Count; i++)
            {
                while (u < ItemsSourceProtected.Count)
                {
                    // Note:
                    // Items that are equal will always have equal visibility
                    // because the same predicate has been applied. That is, 
                    // there's no chance that 'instance1' of remove would 
                    // be mistaken for 'instance2' for this reason, provided
                    // you are going by mapped index instead of  IndexOf().
                    if (Equals(PreChangeSnapshot[i], ItemsSourceProtected[u]))
                    {
                        indexMap[i] = u++;
                        break;
                    }
                    u++;
                }
            }
            return indexMap;
        }
        protected List<T> ItemsSourceProtected { get; } = new List<T>();

        event EventHandler? ISuppressibleEventSource.EventSuppressed
        {
            add => value += OnCollectionChangedSuppressed;
            remove => value -= OnCollectionChangedSuppressed;
        }

        protected virtual void OnCollectionChangedSuppressed(object? sender, EventArgs eUnk)
        {
            if(eUnk is NotifyCollectionChangedEventArgs e)
            {
                EventSuppressed?.Invoke(this, e);
            }
            else
            {
                this.ThrowHard<InvalidCastException>(
                    $"The received {eUnk.GetType().FullName} is not assignable to {nameof(NotifyCollectionChangedEventArgs)}.");

            }
        }

        public event EventHandler? EventSuppressed;
    }
}
