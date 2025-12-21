using IVSoftware.Portable.Collections.Dictionaries;
using System.Collections;

namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// Defines a lookup behavior that semantically enforces the return value.
    /// </summary>
    public interface IInsistent { }

    /// <summary>
    /// Defines a lookup behavior that semantically enforces a non-null Value.
    /// </summary>
    public interface IInsistentDictionary
        : IDictionary
        , IObservableDictionary
        , IInsistent
    { 
        Delegate? ActivationDlgt { get; set; }
    }

    /// <summary>
    /// Defines a lookup behavior that semantically enforces a non-null ValueT.
    /// </summary>
    public interface IInsistentDictionary<TKey, TValue>
        : IInsistentDictionary
        , IObservableDictionary<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        /// <summary>
        /// Provides an optional activation delegate used to create default entries on demand.
        /// </summary>
        /// <remarks>
        /// The Insistent and Tolerant modes impose stricter semantics, but the bottom
        /// line is that any IObservableDictionary with this delegate set will invoke 
        /// it before raising the CollectionChanging event.
        /// </remarks>
        new Func<TValue>? ActivationDlgt { get; set; }
    }
}
