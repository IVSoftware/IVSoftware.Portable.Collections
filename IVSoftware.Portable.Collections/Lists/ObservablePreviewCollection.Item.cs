using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> 
        : ObservableCollection<T>
        , INotifyCollectionChanging
        , ISuppressibleEventSource
    {
#if false
        // Protected mutation virtuals (core pipeline).
        // Preview semantics don't apply because they're not exposed publicly.
        protected override void InsertItem(int index, T item)
        { 
            base.InsertItem(index, item);
        }
        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
        }
        protected override void RemoveItem(int index)
        { 
            base.RemoveItem(index);
        }
        protected override void MoveItem(int oldIndex, int newIndex)
        { 
            base.MoveItem(oldIndex, newIndex);
        }
        protected override void ClearItems()
        { 
            base.ClearItems(); 
        }
#endif

        /// <summary>
        /// Public, preeminent suspension.
        /// </summary>
        public DisposableHost DHostSuppress
        {
            get
            {
                if (_dhostSuppress is null)
                {
                    _dhostSuppress = new DisposableHost(nameof(DHostSuppress));
                    _dhostSuppress.BeginUsing += (sender, e) =>
                    {
                    };
                    _dhostSuppress.FinalDispose += (sender, e) =>
                    {
                        WDTReconcileFilters.StartOrRestart();
                    };
                }
                return _dhostSuppress;
            }
        }
        DisposableHost? _dhostSuppress = null;
        public IDisposable Suppress(SuppressionFlag suppress = SuppressionFlag.All) => DHostSuppress.GetToken(sender: suppress);

        public SuppressionFlag Suppressed
        {
            get 
            {
                SuppressionFlag or = 0;
                foreach (var flag in DHostSuppress.Tokens.Select(_ => _.Sender).OfType<SuppressionFlag>())
                {
                    or |= flag;
                }
                return or;
            }
        }


        /// <summary>
        /// When Filtered, suspends the Visible->Internal realtime tracking 
        /// in order to push Internal->Visible like restore all.
        /// </summary>
        internal DisposableHost DHostSuspendTracking { get; } = new DisposableHost(nameof(DHostSuspendTracking));

        public DisposableHost DHostUIActivity { get; } = new(nameof(DHostUIActivity));


        /// <summary>
        /// State-aware replacement that prepares markdown context for filtering.
        /// </summary>
        public IEnumerable<T> Recordset 
        {
            set
            {
                NotifyCollectionChangingEventArgs ePre;
                
                ePre = new(
                    action: NotifyCollectionChangingAction.Remove,
                    changedItems: this.ToArray(),
                    startingIndex: 0);
                OnCollectionChanging(ePre);
                if(ePre.Cancel)
                {
                    // This would be an unlikely but technically
                    // legal preemptive cancel on the remove phase.
                    return;
                }
                // Now take 'value' and load it up as a
                // (potentially) multi item add range.
                ePre = new(
                    action: NotifyCollectionChangingAction.Add,
                    changedItems: value.ToArray(),
                    startingIndex: 0);

                // Special case. This is more about "opportunity to dispose" and less about cancelling.
                // Don't call OnCollectionChanging (which calls ApplyChanges automatically, but without modifying state).
                CollectionChanging?.Invoke(this, ePre);
                // But we still have to check.
                if (!ePre.Cancel)
                {
                    base.Clear();
                    foreach (var item in ePre.NewItems?.Cast<T>() ?? [])
                    {
                        base.Add(item);
                    }
                    if(_markdownContext is not null)
                    {
                        _markdownContext.SetProtectedSearchState(
                            ePre.NewItems?.Count > 0
                            ? SearchEntryState.QueryCompleteWithResults
                            : SearchEntryState.QueryCompleteNoResults);

                        if (_markdownContext.QueryFilterConfig.HasFlag(QueryFilterConfig.Filter))
                        {
                            _markdownContext.SetProtectedFilteringState(
                            ePre.NewItems?.Count > 1
                            ? FilteringState.Armed
                            : FilteringState.Ineligible);
                        }
                    }
                    using (Distinctifier.BeginAtomic())
                    {
                        Distinctifier.Clear();
                        foreach (var item in ePre.NewItems?.Cast<T>() ?? [] )
                        {
                            Distinctifier.Add(item);
                        }
                    }

                    if (OptimizationMode.HasFlag(ListOptimizationMode.TrackItemPropertyChanges))
                    {
                        ManageItemSubscriptions(ePre);
                    }
                    OnCollectionChanged(ePre.CopyToChangedEvent());
                }
            }
        }
    }
}
