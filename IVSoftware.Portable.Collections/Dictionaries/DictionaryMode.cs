using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    public enum DictionaryMode
    {
        /// <summary>
        /// Normal behavior in every respect.
        /// </summary>
        /// <remarks>
        /// - IDictionary
        /// - IDictionary generic with TKey and TValue where TKey : not null.
        /// </remarks>
        Normal = BehaviorMode.Normal,

        /// <summary>
        /// Tolerates KeyNotFound by returning default without raising exceptions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Tolerant pattern allows safe pattern matching as an alternative to TryGetValue.
        /// - IDictionary
        /// - IDictionary generic with TKey and TValue 
        ///   where TKey : not null
        /// - ITolerant
        /// - ITolerantDictionary
        /// - ITolerantDictionary generic with TKey and TValue 
        ///   where TKey : not null</para>
        /// <para>
        /// Example:
        /// <c>if(tolerant[SomeKey] is { } exists){ ... }</c> 
        /// </remarks></para>
        /// <para>
        /// PREEMINENT
        ///    Raises the CollectionChanging event because the client asked for 'A' (a key that turned 
        ///    out to be non-existent) which has now been replaced by 'null' (in essence). This event is
        ///    coercable, and the client is free to provide a value instead of taking the null.
        ///    </para>
        TolerantReturnDefault = BehaviorMode.TolerantReturnDefault,

        /// <summary>
        /// Tolerates KeyNotFound by adding a new default entry without raising exceptions.
        /// </summary>
        /// <remarks>
        /// This tolerant variant is ideal e.g. for caching "missed attempts."
        /// </remarks>
        TolerantCreateDefaultEntry = BehaviorMode.TolerantCreateDefaultEntry,

        /// <summary>
        /// Insists upon returning an non-null instance of TValue with heuristic fallbacks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// - IDictionary
        /// - IDictionary generic with TKey and TValue where TKey : not null.
        /// - IInsistent
        /// - IInsistentDictionary
        /// - IInsistentDictionary generic with TKey and TValue 
        ///   where TKey : not null
        ///   where TValue : notnull  // <- DIFFERENT</para>
        /// <para>
        /// The Insistent pattern is designed to return the TValue requested "without fail". At 
        /// the same time, it does not abide attempts to create keys that have null values attached.
        /// Heuristic sequence:
        /// 1. Looks for factory delegate in the get signature.
        /// <c>[Indexer]public virtual TValue this[TKey key, StrongTypedDictionaryDlgt @default]</c>
        /// 2. Runs the DefaultActivationDelegate if available. (This usually, but not always,
        ///    references the DefaultActivationType property.
        /// 3. If TValue is non-abstract, retrieves and caches a parameterless CTor by reflection if 
        ///    available, creates an instance from that, and if success stores it as the default Activator.
        /// 4. PREEMINENT
        ///    Raises the CollectionChanging event because the client asked for 'A' (a key that turned 
        ///    out to be non-existent) which has now been replaced by 'B' (a heuristically-created instance).
        /// </para>
        /// </remarks>
        InsistentNotNull = BehaviorMode.InsistentNotNull,

        /// <summary>
        /// Brisk is a NON-GENERIC contract that GUARANTEES an IDictionary instance for any key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Brisk was originally developed in support of Type Exchange Abstraction (TEA) patterns
        /// (thus the name) and (as a broad concept) was originally designed for reflection caching.</para>
        /// <para>
        /// The Brisk pattern supports Complex Keys - basically a getter where:</para>
        /// <c>[Indexer]IDictionary this[object key, params object[] moreKeys] { get; }</c>
        /// </para><para>
        /// Expressed in this manner, the return will be IDictionary where TKey and TValue are 
        /// both object. While it can be surprisingly useful to leave it just like that, various
        /// strongly-typed options are supported by Brisk and are easily accessible
        /// </para>
        /// </para><para>
        /// The Brisk pattern also lends itself to [StdComplexKey] named enums that map friendly
        /// names to strongly-typed dicts with (otherwise) complex keys.
        /// </para>
        /// </remarks>
        Brisk,
    }
}
