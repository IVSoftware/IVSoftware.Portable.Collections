using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Disposable;
using System.Collections;
using System.Collections.Specialized;

public interface IObservableDictionary
    : IDictionary
    , INotifyCollectionChanging
    , INotifyCollectionChanged
{
    void AddRange(IEnumerable<DictionaryEntryPreview> entries);

    /// <summary>
    /// Virtual property where subclasses declare their modus operandi.
    /// </summary>
    /// <remarks>
    /// Each dictionary's got its own M.O. - the Tolerant one's the easygoing accomplice
    /// who lets everything slide, Insistent is the hard-liner who never leaves without 
    /// a value, and Brisk? That's the fast-talker with a hundred aliases and a getaway plan. 
    /// </remarks>
    DictionaryMode Mode { get; }

    DisposableHost DHostEphemeralMode { get; }
}
public interface IObservableDictionary<TKey, TValue>
    : IObservableDictionary
    , IDictionary<TKey, TValue>
    where TKey : notnull
{
}

internal interface IUpgradeableDictionary
    : IObservableDictionary
{
    (int countChanging, int countChanged) TransferEvents(IObservableDictionary to);
}