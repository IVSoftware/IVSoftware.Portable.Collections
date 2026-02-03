using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Common.Exceptions;
using System.Collections;
using System.Reflection;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> : IRangeable
    {
        private bool TryGetSafeBuffer(IEnumerable? newItems, out IList<T> itemsT, out object? errorItem)
        {
            if (newItems is null)
            {
                itemsT = null!;
                errorItem = null;
                return false;
            }
            else
            {
                itemsT = new List<T>();
                errorItem = null;

                foreach (var item in newItems.Cast<object?>())
                {
                    if (item.IsAssignableAs(out T? valueT))
                    {
                        itemsT.Add(valueT!);
                    }
                    else
                    {
                        // Return immediately. Prevent partial add
                        itemsT = default!;
                        errorItem = item;
                        return false;
                    }
                }
                return true;
            }
        }

        void IRangeable.AddRange(IEnumerable items)
        {
            if(TryGetSafeBuffer(items, out IList<T> itemsT, out object? errorItem))
            {
                AddRange(itemsT);
            }
            else
            {
                this.ThrowHard<InvalidCastException>(
                    $"Cannot coerce item '{errorItem}' to {typeof(T).Name}.");
            }
        }


        int IRangeable.AddRangeDistinct(IEnumerable items)
        {
            if (TryGetSafeBuffer(items, out IList<T> itemsT, out object? errorItem))
            {
                return AddRangeDistinct(itemsT);
            }
            else
            {
                this.ThrowHard<InvalidCastException>(
                    $"Cannot coerce item '{errorItem}' to {typeof(T).Name}.");
                return 0;
            }
        }
        public void InsertRange(int startingIndex, IEnumerable items)
        {
            if (TryGetSafeBuffer(items, out IList<T> itemsT, out object? errorItem))
            {
                this.InsertRange(startingIndex, itemsT);
            }
            else
            {
                this.ThrowHard<InvalidCastException>(
                    $"Cannot coerce item '{errorItem}' to {typeof(T).Name}.");
            }
        }

        public void RemoveRange(int startingIndex, int endingIndex)
        {
            if (startingIndex == -1
                || startingIndex >= Count
                || endingIndex == -1
                || endingIndex >= Count
                )
            {
                this.ThrowHard<IndexOutOfRangeException>($"Starting and Ending indexes must be less than {Count}");
            }
            else
            {
                NotifyCollectionChangingEventArgs ePre = new(
                    action: NotifyCollectionChangingAction.Remove,
                    changedItems: new[] { new CollectionRange(startingIndex, endingIndex) });
                OnCollectionChanging(ePre);
            }
        }

        public int RemoveMultiple(IEnumerable items)
        {
            if (TryGetSafeBuffer(items, out IList<T> newItemsT, out object? errorItem))
            {
                return this.RemoveMultiple(newItemsT);
            }
            else
            {
                this.ThrowHard<InvalidCastException>($"The {nameof(items)} argument contains non-assignable objects.");
                return 0;
            }
        }
    }
    public partial class ObservablePreviewCollection<T> : IRangeable<T>
    {
        public void AddRange(IEnumerable<T> items)
        {
            NotifyCollectionChangingEventArgs ePre = new (
                action: NotifyCollectionChangingAction.Add,
                changedItems: (IList)items,
                index: Count,
                oldIndex: -1);
            OnCollectionChanging(ePre);
        }

        /// <summary>
        /// Add a range of new items, allowing only distinct elements into the list.
        /// </summary>
        [Careful("The items list itself must be made distinct for this to work.")]
        public int AddRangeDistinct(IEnumerable<T> items)
        {
            // CRITICAL:
            // The items are being tested against Distinctifier, which is NOT
            // being updated in this preview phase. This means that if items 
            // contains two of the SAME item - one that isn't already in the
            // distinctifier - then it's going to pass BOTH. That's not what
            // we want. To avoid this, we distinct the items FIRST.
            var distinct = new HashSet<T>();
            foreach(var item in items)
            {
                distinct.Add(item);
            }
            List<T> allowedItems = new List<T>();
            if (OptimizationMode.HasFlag(ListOptimizationMode.UseCacheForContains))
            {
                foreach (var item in distinct)
                {
                    // At this stage, preview by checking contains, not by 'trying Add'.
                    if (Distinctifier.Contains(item))
                    {   /* G T K */
                        // Prohibited.
                    }
                    else
                    {
                        allowedItems.Add(item);
                    }
                }
            }
            else
            {
                allowedItems.AddRange(items);
                foreach (var exists in this) // Does not modify this
                {
                    while (allowedItems.Contains(exists))
                    {
                        allowedItems.Remove(exists);
                        // Fast track if there are 0 non-duplicates.
                        if (allowedItems.Count == 0)
                        {
                            return 0;
                        }
                    }
                }
            }
            NotifyCollectionChangingEventArgs ePre = new(
                action: NotifyCollectionChangingAction.Add,
                changedItems: (IList)allowedItems);
            OnCollectionChanging(ePre);
            return ePre.GetAppliedChangesCount();
        }

        public void InsertRange(int startingIndex, IEnumerable<T> newItems)
        {
            if (startingIndex == -1 || startingIndex >= Count)
            {
                this.ThrowHard<IndexOutOfRangeException>();
            }
            else
            {
                NotifyCollectionChangingEventArgs ePre = new(
                    action: NotifyCollectionChangingAction.Add,
                    changedItems: (IList)newItems,
                    startingIndex: startingIndex);
                OnCollectionChanging(ePre);
            }
        }

        public int RemoveMultiple(IEnumerable<T> items)
        {
            NotifyCollectionChangingEventArgs ePre = new(
                action: NotifyCollectionChangingAction.Remove,
                changedItems: items.Cast<object?>().ToList());
            OnCollectionChanging(ePre);
            if (ePre.Cancel)
            {
                return 0;
            }
            else
            {
                return ePre.GetAppliedChangesCount();
            }
        }
    }
}
