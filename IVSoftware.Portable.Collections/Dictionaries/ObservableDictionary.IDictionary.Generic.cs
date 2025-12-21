using IVSoftware.Portable.Threading;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    public partial class ObservableDictionary<TKey, TValue>
    : IDictionary<TKey, TValue>
    {
        // Backing dictionary (your real implementation)

        //---------------------------------------------------------
        // Explicit IDictionary<TKey, TValue>
        //---------------------------------------------------------

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                var result = this[key];
                this.OnAwaited();
                return result;
            }
            set
            {
                this[key] = value;
                this.OnAwaited();
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                var result = this.Keys;
                this.OnAwaited();
                return result;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                var result = this.Values;
                this.OnAwaited();
                return result;
            }
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            this.Add(key, value);
            this.OnAwaited();
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            var result = this.ContainsKey(key);
            this.OnAwaited();
            return result;
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            var result = this.Remove(key);
            this.OnAwaited();
            return result;
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            var result = this.TryGetValue(key, out value);
            this.OnAwaited();
            return result;
        }

        //---------------------------------------------------------
        // Explicit ICollection<KeyValuePair<TKey,TValue>>
        //---------------------------------------------------------

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item);
            this.OnAwaited();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            this.Clear();
            this.OnAwaited();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            var result = this.Contains(item);
            this.OnAwaited();
            return result;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
            KeyValuePair<TKey, TValue>[] array,
            int arrayIndex)
        {
            this.CopyTo(array, arrayIndex);
            this.OnAwaited();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = this.Remove(item);
            this.OnAwaited();
            return result;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                var result = this.IsReadOnly;
                this.OnAwaited();
                return result;
            }
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get
            {
                var result = this.Count;
                this.OnAwaited();
                return result;
            }
        }

        //---------------------------------------------------------
        // Explicit IEnumerable
        //---------------------------------------------------------

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            var result = this.GetEnumerator();
            this.OnAwaited();
            return result;
        }
    }
}
