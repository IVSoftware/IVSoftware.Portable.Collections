
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.SQLiteMarkdown;
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
                    _markdownContext = new MarkdownContext<T>();
                    _markdownContext.PropertyChanged += (sender, e) =>
                    {
                        Debug.WriteLine($"251227.A {e.PropertyName}");
                        switch (e.PropertyName)
                        {
                            case nameof(_markdownContext.IsFiltering):
                                // [Careful]
                                // When QueryFilterConfig is Filter only, clearing or
                                // backspace-to-clear does *not* set IsFiltering to false.
                                break;
                            case nameof(_markdownContext.FilteringState):
                                if (_markdownContext.FilteringState == FilteringState.Active)
                                {
                                    // Potentially capture snapshot.
                                    IsFiltering = true;
                                }
                                else
                                {
                                    // Potentially revert to full.
                                    if (ActiveFilters.Count == 0)
                                    {
                                        IsFiltering = false;
                                    }
                                    OnPropertyChanged(nameof(ActiveFilters));
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
        MarkdownContext<T>? _markdownContext = null;


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
