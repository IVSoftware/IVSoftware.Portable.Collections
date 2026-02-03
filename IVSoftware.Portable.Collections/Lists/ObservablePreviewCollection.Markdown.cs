
using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using SQLite;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

namespace IVSoftware.Portable.Collections.Lists
{
    partial class ObservablePreviewCollection<T>
    {
        /// <summary>
        /// Markdown context singleton. 
        /// </summary>
        [Careful("Null *is* a normal operating state e.g. for value types.")]
        public MarkdownContext<T>? MarkdownContext
        {
            get
            {
                if (_markdownContext is null 
                    && IsTNew && TryGetPrimaryKeyProperty(out _))
                {
                    _markdownContext = new MarkdownContextInternal<T>();
                    _markdownContext.PropertyChanged += (sender, e) =>
                    {
                        Debug.WriteLine($"251227.A {e.PropertyName}");
                        switch (e.PropertyName)
                        {

#if ABSTRACT
            FROM SQLiteMarkdown Demo            

            // Relies on BC functionality, except where firing the CollectionChanged event is concerned.
            base.OnFilteringStateChanged();

            // List-specific.
            switch (FilteringState)
            {
                case FilteringState.Ineligible:
                    // Clear, then event ADHOC. That is, it's not always
                    // in our best interest to simply forward the clear.
                    _unfilteredItems.Clear();
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    break;
                case FilteringState.Armed:
                    if (FilteringStatePrev == FilteringState.Ineligible)
                    {
                        await Task.Delay(TimeSpan.FromTicks(1));
                        FilterQueryDatabase.DeleteAll<T>();
                        FilterQueryDatabase.InsertAll(_unfilteredItems);
                    }
                    break;
                case FilteringState.Active:
                    break;
            }
#endif
                            case nameof(_markdownContext.IsFiltering):
                                // [Careful]
                                // When QueryFilterConfig is Filter only, clearing or
                                // backspace-to-clear does *not* set IsFiltering to false.
                                break;
                            case nameof(_markdownContext.FilteringState):
                                switch (_markdownContext.FilteringState)
                                {
                                    case FilteringState.Ineligible:
                                        Clear();
                                        _markdownContext.SetProtectedSearchState(SearchEntryState.Cleared);
                                        break;
                                    case FilteringState.Armed:
                                        // Potentially revert to full.
                                        if (ActiveFilters.Count == 0)
                                        {
                                            IsFiltering = false;
                                        }
                                        OnPropertyChanged(nameof(ActiveFilters));
                                        break;
                                    case FilteringState.Active:
                                        // Potentially capture snapshot.
                                        IsFiltering = true;
                                        break;
                                    default:
                                        this.ThrowHard<NotSupportedException>($"The {_markdownContext.FilteringState.ToFullKey()} case is not supported.");
                                        break;
                                }
                                if (_markdownContext.FilteringState == FilteringState.Active)
                                {
                                }
                                else
                                {
                                }
                                break;
                            case nameof(_markdownContext.SearchEntryState):
                                switch (_markdownContext.SearchEntryState)
                                {
                                    case SearchEntryState.Cleared:
                                        Clear();
                                        break;
                                    default:
                                        /* G T K */
                                        // N O O P
                                        break;
                                }
                                break;
                            case nameof(MarkdownContext.InputText):
                                // Use the WDT for this class instead of
                                // awaiting the InputTextSettled event.
                                WDTReconcileFilters.StartOrRestart();
                                break;
                        }
                    };
                }
                // Don't throw here. Depending on T the MDC can quietly
                // remove itself from consideration. The correct approach
                // for critical uses is to null-check inline.
                return _markdownContext;
            }
        }
        MarkdownContextInternal<T>? _markdownContext = null;


        public SQLiteConnection FilterDB
        {
            get
            {
                if (TryGetPrimaryKeyProperty(out _))
                {
                    if (_filterDB is null)
                    {
                        _filterDB = new SQLiteConnection(":memory:");
                        _filterDB.CreateTable<T>();
                    }
                }
                else
                {
                    this.ThrowSoft<InvalidOperationException>("Unnecessary singleton instantiation for FilterDB for ineligible config.");
                }
                return _filterDB!;
            }
        }
        SQLiteConnection? _filterDB = null;
    }
}
