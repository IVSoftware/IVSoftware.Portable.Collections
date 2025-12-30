using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Disposable;
using System.Collections.ObjectModel;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> 
        : ObservableCollection<T>
        , INotifyCollectionChanging
        , ISuppressibleEventSource
    {
        // Protected mutation virtuals (core pipeline)
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
    }
}
