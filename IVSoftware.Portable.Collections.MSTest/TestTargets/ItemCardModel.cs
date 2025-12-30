using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.SQLiteMarkdown.Common;
using System.ComponentModel;
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;

namespace IVSoftware.Portable.Collections.MSTest.TestTargets
{
    class ItemCardModel : SelectableQFModel, INotifyPropertyChanging
    {
        public bool ShowCheckboxes
        {
            get
            {
                var e = new CancelEventArgs();
                BeforeShowCheckboxes?.Invoke(this, e);
                if (e.Cancel)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static event CancelEventHandler? BeforeShowCheckboxes;
        public event PropertyChangingEventHandler? PropertyChanging;


        [Track(TrackMode.Single, WherePredicate.IsNotZero)]
        public new ItemSelection Selection
        {
            get => base.Selection;
            set
            {
                var e = new PropertyChangingPreviewEventArgs<ItemSelection>(
                    oldValue: base.Selection,
                    newValue: value);
                PropertyChanging?.Invoke(this, e);
                if (!e.Cancel)
                {
                    base.Selection = e.NewValue;
                }
            }
        }

        [Track(TrackMode.Multiple, WherePredicate.IsTrue)]
        public new bool IsChecked
        {
            get => base.IsChecked;
            set
            {
                var e = new PropertyChangingPreviewEventArgs<bool>(
                    oldValue: base.IsChecked,
                    newValue: value);
                PropertyChanging?.Invoke(this, e);
                if (!e.Cancel)
                {
                    base.IsChecked = e.NewValue;
                }
            }
        }
    }
}
