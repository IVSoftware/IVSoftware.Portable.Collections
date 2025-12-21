using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Threading;
using System.Collections;
using System.Collections.Specialized;

namespace IVSoftware.Portable.Collections.Dictionaries
{    

    public partial class ObservableDictionary<TKey, TValue>
        : IDictionary
    {
        [Careful("The target in this case is 'this' not '@base'.")]
        object? IDictionary.this[object key]
        {
            get
            {
                this.OnAwaited(); // test hook

                if (key is TKey keyT)
                {
                    return this[keyT];
                }
                else
                {
                    this.ThrowHard<InvalidCastException>(
                        $"Key must be of type {typeof(TKey).Name}, but was {key?.GetType().Name ?? "null"}.");
                    return null;
                }
            }
            set
            {
                this.OnAwaited(); // test hook

                if (!key.IsAssignableAs(out TKey? keyT))
                {
                    this.ThrowHard<InvalidCastException>(
                        $"Key must be of type {typeof(TKey).Name}, but was {key?.GetType().Name ?? "null"}.");
                    return;
                }
                if (!value.IsAssignableAs(out TValue? valueT))
                {
                    this.ThrowHard<InvalidCastException>(
                        $"Value must be of type {typeof(TValue).Name}, but was {value?.GetType().Name ?? "null"}.");
                    return;
                }

                ((IDictionary<TKey, TValue?>)this)[keyT!] = valueT;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                this.OnAwaited(); // test hook
                return false;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                this.OnAwaited(); // test hook
                return @base.Keys;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                this.OnAwaited(); // test hook
                return @base.Values;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                this.OnAwaited(); // test hook
                return IsReadOnly;
            }
        }

        int ICollection.Count
        {
            get
            {
                this.OnAwaited(); // test hook
                return Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                this.OnAwaited(); // test hook
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                this.OnAwaited(); // test hook
                return ((ICollection)@base).SyncRoot;
            }
        }

        [Careful("The target in this case is 'this' not '@base'.")]
        void IDictionary.Add(object key, object? value)
        {
            this.OnAwaited(); // test hook

            if (key.IsAssignableAs<TKey>(out var keyT) &&
                value.IsAssignableAs<TValue>(out var valueT))
            {
                ((IDictionary<TKey, TValue?>)this).Add(keyT!, valueT!);
            }
            else
            {
                this.ThrowHard<InvalidCastException>(
                    $"Key must be {typeof(TKey).Name} and value must be {typeof(TValue).Name}.");
            }
        }

        bool IDictionary.Contains(object key)
        {
            this.OnAwaited(); // test hook

            return key is TKey keyT && @base.ContainsKey(keyT);
        }

        [Careful("The target in this case is 'this' not '@base'.")]
        void IDictionary.Remove(object key)
        {
            this.OnAwaited(); // test hook

            if (key.IsAssignableAs(out TKey? keyT))
            {
                this.Remove(keyT!);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.OnAwaited(); // test hook

            ((ICollection)@base).CopyTo(array, index);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            this.OnAwaited(); // test hook

            return ((IDictionary)@base).GetEnumerator();
        }

        void IDictionary.Clear()
        {
            this.OnAwaited(); // test hook

            this.Clear();
        }
    }
}
