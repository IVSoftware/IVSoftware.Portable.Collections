using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Threading;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO.Pipes;
using System.Windows.Input;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> 
        : ObservableCollection<T>
        , ISuppressibleEventSource
        , IContainerBindingContext
    {
        public object? ContainerBindingContext
        {
            get => _containerBindingContext;
            set
            {
                if (!Equals(_containerBindingContext, value))
                {
                    _containerBindingContext = value;
                    OnPropertyChanged();
                }
            }
        }
        object? _containerBindingContext = default;


        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.OnAwaited();

            if (DHostSuppress.IsZero()) // Public, preeminent suppression.
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var ecc in e.NewItems?.OfType<IContainerBindingContext>() ?? [])
                        {
                            ecc.ContainerBindingContext = this;
                        }
                        break;
                }
                // UPGRADE 251126
                if (e is NotifyPreviewCollectionChangedEventArgs)
                {
                    if(IsFiltering == true && DHostSuspendTracking.IsZero())
                    {
                        TrackVisibleCollectionChanges(e);
                    }
#if DEBUG
                    // Internal testing - in DEBUG mode - only.
                    // UnitTesting is responsible for visibility on and coverage for this edge case.
                    if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == -1)
                    {
                        Debug.WriteLine($@"ADVISORY - This condition can lead to a procedural failure in MAUI collection.");
#if ABSTRACT
{FDA1ED69-E0BC-4ABC-BA87-7D4FF5BEB318}
namespace Microsoft.Maui.Controls
{
	public class MarshalingObservableCollection : List<object>, INotifyCollectionChanged
	{
        void Add(NotifyCollectionChangedEventArgs args)
        {
	        var startIndex = args.NewStartingIndex;
	        foreach (var item in args.NewItems)
	        {
		        Insert(startIndex, item);
		        startIndex += 1;
	        }

	        OnCollectionChanged(args);
        }
        ...
    }
}
#endif

                    }
#endif


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
            if (TrackContexts.Any())
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
                            UnfilteredItemsProtected.Insert(newStartingIndex++, item);
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
                                    UnfilteredItemsProtected.RemoveAt(i);
                                }
                            }
                            else
                            {
                                if (TryGetPrimaryKeyProperty(out var pi))
                                {
                                    // Remove by object
                                    foreach (T item in e.OldItems)
                                    {
                                        for (int uu = 0; uu < UnfilteredItemsProtected.Count; uu++)
                                        {
                                            if (pi.GetValue(item)?.ToString() is { } id &&
                                                id == pi.GetValue(UnfilteredItemsProtected[uu])?.ToString())
                                            {
                                                UnfilteredItemsProtected.RemoveAt(uu);
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
                                        UnfilteredItemsProtected.Remove(item);
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
                                if (u is null || u is -1 || u >= UnfilteredItemsProtected.Count)
                                {
                                    // Post-translation error
                                    this.ThrowHard<IndexOutOfRangeException>(
                                        $"Cannot remove item at translated index={u}");
                                    anyError = true;
                                }
                                else
                                {
                                    UnfilteredItemsProtected.RemoveAt((int)u);
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
                        if (u is null || u is -1 || u >= UnfilteredItemsProtected.Count)
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
                                UnfilteredItemsProtected[(int)u] = (T)e.NewItems[0]!;
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
                            from >= UnfilteredItemsProtected.Count ||
                            to >= UnfilteredItemsProtected.Count)
                        {
                            this.ThrowHard<InvalidOperationException>(
                                "Index mapping failed. Visible and canonical collections are out of sync.");
                            anyError = true;
                            break;
                        }
                        // Extract the item
                        var move = UnfilteredItemsProtected[(int)from];
                        UnfilteredItemsProtected.RemoveAt((int)from);
                        // If the item was removed before the insertion point,
                        // the target index shifts left by one
                        if (from < to)
                        {
                            to--;
                        }
                        UnfilteredItemsProtected.Insert((int)to, move);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        UnfilteredItemsProtected.Clear();
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
                        foreach (T item in UnfilteredItemsProtected)
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
            for (int i = 0; i < PreChangeSnapshot.Length && u < UnfilteredItemsProtected.Count; i++)
            {
                while (u < UnfilteredItemsProtected.Count)
                {
                    // Note:
                    // Items that are equal will always have equal visibility
                    // because the same predicate has been applied. That is, 
                    // there's no chance that 'instance1' of remove would 
                    // be mistaken for 'instance2' for this reason, provided
                    // you are going by mapped index instead of  IndexOf().
                    if (Equals(PreChangeSnapshot[i], UnfilteredItemsProtected[u]))
                    {
                        indexMap[i] = u++;
                        break;
                    }
                    u++;
                }
            }
            return indexMap;
        }

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
