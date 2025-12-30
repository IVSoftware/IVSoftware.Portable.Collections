using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.SQLiteMarkdown.Common;
using System.ComponentModel;
using System.Windows.Input;
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;

namespace FilteredList.Maui.Demo.Models
{
    class ItemCardModel : SelectableQFModel, INotifyPropertyChanging
    {
        internal static readonly ItemSelection PRESSED = (ItemSelection)0x8;
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

        /// <summary>
        /// Bindable selection that raises visual state changes for
        ///pressed without interfering with the tracking state itself.
        /// </summary>
        [Track(TrackMode.Single, WherePredicate.IsNotZero)]
        public new ItemSelection Selection
        {
            get => base.Selection;
            set
            {
                if (!Equals(Selection, value))
                {
                    var e = new PropertyChangingPreviewEventArgs<ItemSelection>(
                        oldValue: base.Selection,
                        newValue: value,
                        propertyName: nameof(PressedSelection));
                    PropertyChanging?.Invoke(this, e);
                    if (!e.Cancel)
                    {
                        base.Selection = e.NewValue;
                        OnPropertyChanged(nameof(PressedSelection));
                    }
                }
            }
        }

        /// <summary>
        /// Helper property for tracking visual state when item is pressed.
        /// </summary>
        public ItemSelection PressedSelection => IsPressed ? Selection | PRESSED: Selection;

        public bool IsPressed
        {
            get => _isPressed;
            internal set
            {
                if (!Equals(_isPressed, value))
                {
                    _isPressed = value;
                    OnPropertyChanged(nameof(PressedSelection));
                }
            }
        }
        bool _isPressed = false;




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
