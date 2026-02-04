using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.SQLiteMarkdown.Common;
using Newtonsoft.Json;
using SQLite;
using System.ComponentModel;
using System.Windows.Input;
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;

namespace OPC.Preview.Portable.Models
{
    [Table(nameof(ItemCardModel)), EditorTemplate(template: typeof(ItemCardModelEditorConfiguration))]
    public class ItemCardModel 
        : SelectableQFModel
        , INotifyPropertyChanging
        ,IOPAmbientBindingContext
    {
        /// <summary>
        /// Bindable selection that raises visual state changes for
        ///pressed without interfering with the tracking state itself.
        /// </summary>
        [Track(TrackMode.Single, WherePredicate.IsNotZero)]
        [Ignore]
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
                        if((e.NewValue & (ItemSelection)0xF0) == 0)
                        {
                            base.Selection = e.NewValue;
                        }
                        else
                        {
                            if(e.NewValue.HasFlag((ItemSelection)TrackStateEphemeral.NotPressed))
                            {
                                IsPressed = false;
                            }
                        }
                        OnPropertyChanged(nameof(PressedSelection));
                    }
                }
            }
        }

        /// <summary>
        /// Helper property for tracking visual state when item is pressed.
        /// </summary>
        public ItemSelection PressedSelection =>
            IsPressed 
            ? Selection | (ItemSelection)TrackStateEphemeral.Pressed
            : Selection;

        public bool IsPressed
        {
            get => _isPressed;
            set
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

        public bool HasKeywordOrTag => !string.IsNullOrWhiteSpace(KeywordsDisplay) || !string.IsNullOrWhiteSpace(Tags);

        /// <summary>
        /// Get - remove the outer []
        /// Set - tokenize and serialize as json.
        /// </summary>
        public new string KeywordsDisplay
        {
            get => string.Join(", ", JsonConvert.DeserializeObject<List<string>>(Keywords) ?? []);
            set
            {
                var preview = value.KeywordsEntryToJson();

                if (!Equals(Keywords, preview))
                {
                    Keywords = preview;
                    OnPropertyChanged();
                    // [Careful]
                    // - ALSO make the second line grid
                    //   visible if it's not already.
                    OnPropertyChanged(nameof(HasKeywordOrTag));
                }
            }
        }

        public event PropertyChangingEventHandler? PropertyChanging;

        [Ignore]
        public object? AmbientBindingContext
        {
            get => _ambientBindingContext;
            set
            {
                if (!Equals(_ambientBindingContext, value))
                {
                    _ambientBindingContext = value;
                    OnPropertyChanged();
                }
            }
        }
        object? _ambientBindingContext = default;
    }
}
