using IVSoftware.Portable.Collections.Common;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IVSoftware.Portable.Collections.Lists
{
    public enum ListMode
    {
        /// <summary>
        /// Normal behavior in every respect.
        /// </summary>
        Normal = BehaviorMode.Normal,

        /// <summary>
        /// Tolerates KeyNotFound or IndexOutOfRange by returning default without raising exceptions.
        /// </summary>
        /// <remarks>
        TolerantReturnDefault = BehaviorMode.TolerantReturnDefault,

        /// <summary>
        /// Tolerates KeyNotFound or IndexOutOfRange by adding a new default entry without raising exceptions.
        /// </summary>
        /// <remarks>
        /// This tolerant variant is ideal e.g. for caching "missed attempts." 
        /// </remarks>
        [Canonical("Purpose is distinct even though both tolerant modes raise events.")]
        TolerantCreateDefaultEntry = BehaviorMode.TolerantCreateDefaultEntry,

        /// <summary>
        /// Insists upon returning an non-null instance of TValue with heuristic fallbacks. 
        /// Any new not-null instances thus created are then added to the collection.        /// 
        /// </summary>
        InsistentNotNull = BehaviorMode.InsistentNotNull,
    }

    [Flags]
    public enum ListOptimizationMode
    {
        /// <summary>
        /// Normal behavior in every respect.
        /// </summary>
        Normal = BehaviorMode.Normal,

        /// <summary>
        /// Contents are dynamically tracked making Contains a O(1).
        /// </summary>
        UseCacheForContains = 0x1,

        /// <summary>
        /// INotifyPropertyChanged is tracked for items.
        /// </summary>
        TrackItemPropertyChanges = 0x2,
    }

    /// <summary>
    /// Lightweight mapper that eliminates guesswork e.g. as
    /// to how the `Selection` property contributes to filter.
    /// </summary>
    public enum StdPredicate
    {
        /// <summary>
        /// Selected items without regard to e.g. Primary, Multi, Exclusive etc.
        /// </summary>
        [Where("Selection", WherePredicate.IsNotZero)]
        IsSelected,

        /// <summary>
        /// Items that are affirmatively checked. As in "show only the items that are checked".
        /// </summary>
        [Where("IsChecked", WherePredicate.IsTrue)]
        IsChecked,

        /// <summary>
        /// Items that are affirmatively unchecked. As in "show only the items that are unchecked".
        /// </summary>
        [Where("IsChecked", WherePredicate.IsFalse)]
        IsUnchecked,
    }

    public enum FilterIndexer
    {
        /// <summary>
        /// Return all filtered items that match all predicates.
        /// </summary>
        And,

        /// <summary>
        /// Return all filtered items that match any predicates.
        /// </summary>
        Or,

        /// <summary>
        /// Return all unfiltered items that match all predicates.
        /// </summary>
        UnfilteredAnd,

        /// <summary>
        /// Return all unfiltered items that match any predicates.
        /// </summary>
        UnfilteredOr,
    }

    public interface IObservablePreviewCollection
        : IList
        , INotifyCollectionChanging
        , INotifyCollectionChanged
        , INotifyPropertyChanging
        , INotifyPropertyChanged
        , IRangeable
    {
        ListMode Mode { get; }

        ListOptimizationMode OptimizationMode { get; set; }

        bool AddDistinct(object item);

        void Move(int oldIndex, int newIndex);
    }


    public interface IObservablePreviewCollection<T>
        : IObservablePreviewCollection
        , IList<T>
        , IRangeable<T>
    {
        bool AddDistinct(T item);
    }
    public interface IFilterableCollection
    {
        IDisposable BeginFilterAtom();

        IReadOnlyDictionary<string, Enum> ActiveFilters { get; }

        void ActivateFilters(Enum stdPredicate, params Enum[] more);

        void DeactivateFilters(Enum stdPredicate, params Enum[] more);

        void ClearFilters(bool clearInputText = true);

        int CountUnfiltered { get; }

        bool IsFiltering { get; }
    }
    public interface ITrackContext : INotifyPropertyChanged
    {
        public int Count { get; }

        public Array CurrentItems { get; }
    }
}
