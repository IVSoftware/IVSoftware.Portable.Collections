using IVSoftware.Portable.Collections.Dictionaries;
using System.Collections;

namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// Returning this value in a CollectionChangingEventArgs will make an entry or add a value that is null.
    /// </summary>
    public enum TolerantValue { ExplicitNull }

    /// <summary>
    /// Marker for ITolerant patterns.
    /// </summary>
    public interface ITolerant
    {
        /// <summary>
        /// Defines an indexer that tolerates missing keys without raising exceptions.
        /// </summary>
        /// <remarks>
        /// Corresponds to <see cref="DictionaryMode.TolerantReturnDefault"/> and related tolerant modes.
        /// By default, a missing key returns <c>null</c> instead of throwing <see cref="KeyNotFoundException"/>.
        /// Setting <paramref name="throw"/> to <c>true</c> temporarily enforces insistent behavior,
        /// causing an exception if the key is not present.
        /// This allows selective strictness without changing the dictionary's overall mode.
        /// </remarks>
        object? this[object key, bool @throw = false] { get; set; }
    }

    /// <summary>
    /// Combines the tolerant indexer behavior with collection change notifications.
    /// </summary>
    public interface ITolerantDictionary<TKey, TValue>
        : ITolerant
        , IObservableDictionary<TKey, TValue?>
        where TKey : notnull
    {
        TValue? this[TKey key, bool @throw = false] { get; set; }
    }
}
