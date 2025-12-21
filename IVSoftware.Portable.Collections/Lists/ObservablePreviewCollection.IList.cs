using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using System.Collections;
using System.Diagnostics;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> : IObservablePreviewCollection
    {
        public bool AddDistinct(object? item)
        {
            if(item.IsAssignableAs(out T? itemT))
            {
                return AddDistinct(itemT);
            }
            else
            {
                this.ThrowHard<NotSupportedException>("Invalid cast in Add(object?).");
                return false;
            }
        }

        public new void Move(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Count)
            {
                this.ThrowHard<IndexOutOfRangeException>();
                return;
            }

            if (newIndex < 0 || newIndex >= Count)
            {
                this.ThrowHard<IndexOutOfRangeException>();
                return;
            }

            var item = this[oldIndex];
            var ePre = new NotifyCollectionChangingEventArgs(
                action: NotifyCollectionChangingAction.Move,
                changedItem: item,
                index: newIndex,
                oldIndex: oldIndex);

            OnCollectionChanging(ePre);
        }
    }

    public partial class ObservablePreviewCollection<T> : IList
    {
        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                if(value is null)
                {
                    if(Nullable.GetUnderlyingType(typeof(T)) is not null)
                    {
                        this[index] = default!;
                    }
                    else
                    {
                        this.ThrowHard<InvalidCastException>(
                            $"A value of 'null' cannot be assigned to {typeof(T).FullName}");
                    }
                }
                else if (value is T itemT)
                {
                    this[index] = itemT;
                }
                else
                {
                    this.ThrowHard<InvalidCastException>(
                        $"The value set to IList [Indexer] is not assignable to {typeof(T).FullName}");
                }
            }
        }

        /// <summary>
        /// Special case, because IList.Add returns the index added but OC does not.
        /// </summary>
        int IList.Add(object? item)
        {
            if (item.IsAssignableAs(out T? itemT))
            {
                // Capture the pending ePre on the way in
                // because we won't get another chance.
                NotifyCollectionChangingEventArgs? ePre = null;

                #region L o c a l F x
                void localOnNotifyCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
                {
                    ePre = e as NotifyCollectionChangingEventArgs;
                }
                #endregion L o c a l F x
                using (this.WithOnDispose(
                    onInit: (sender, e) =>
                    {
                        this.CollectionChanging += localOnNotifyCollectionChanging;
                    },
                    onDispose: (sender, e) =>
                    {
                        this.CollectionChanging -= localOnNotifyCollectionChanging;
                    }))
                {
                    Add(itemT);
                    if(ePre is null)
                    {
                        Debug.Fail($@"ADVISORY - UNEXPECTED.");
                        return Count - 1;
                    }
                    else
                    {
                        return ePre.Cancel
                            ? -1
                            : ePre.GetAppliedChangesCount() == 1
                                ? Count - 1
                                : -1;
                    }
                }
            }
            else
            {
                this.ThrowHard<InvalidCastException>("Invalid cast in Add(object?).");
                return -1;
            }
        }

        bool IList.Contains(object? item)
        {
            return item.IsAssignableAs(out T itemT) && Contains(itemT);
        }

        int IList.IndexOf(object? item)
        {
            return item is T itemT
                ? IndexOf(itemT) 
                : -1;
        }

        void IList.Insert(int index, object? item)
        {
            if (item.IsAssignableAs(out T? itemT))
            {
                Insert(index, itemT);
            }
            else
            {
                this.ThrowHard<NotSupportedException>("Invalid cast in Insert(object?).");
            }
        }

        void IList.Remove(object? item)
        {
            if (item is T itemT)
            {
                Remove(itemT);
            }
            else
            {
                this.ThrowHard<NotSupportedException>("Invalid cast in Remove(object?).");
            }
        }

        bool IList.IsFixedSize => false;


        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is T[] typed)
            {
                CopyTo(typed, index);
            }
            else
            {
                this.ThrowHard<NotSupportedException>("Invalid array type in CopyTo(Array).");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
