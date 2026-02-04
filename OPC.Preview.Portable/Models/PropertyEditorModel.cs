using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.Xml.Linq;
using OPC.Preview.Portable.Events;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OPC.Preview.Portable.Models
{
    public class PropertyEditorModel 
        : IOPAmbientBindingContext
        , IOPItemEditor
        , INotifyPropertyChanged
    {
        public PropertyEditorModel()
        {
            CommandBarClickableEventCommand = new CommandPCL(OnCommandBarClickableEvent);
            PropertyInfoItems.AmbientBindingContext = this;

            NextControlCommand = new CommandPCL(OnNextControl);
            PrevControlCommand = new CommandPCL(OnPrevControl);
        }


        public object? AmbientBindingContext
        {
            get => _ambientBindingContext;
            set
            {
                if (!Equals(_ambientBindingContext, value))
                {
                    _ambientBindingContext = value;
                    OnAmbientBindingContextChanged();
                    OnPropertyChanged();
                }
            }
        }
        object? _ambientBindingContext = default;

        protected virtual void OnAmbientBindingContextChanged()
        {
#if ABSTRACT
            // In this case, we want to push `this` as
            // the ABC and we do this in the ctor.
            if (AmbientBindingContext is not null)
            {
                PropertyInfoItems.AmbientBindingContext = AmbientBindingContext;
            }
#endif
        }
        public ICommand NextControlCommand { get; }
        protected virtual void OnNextControl(object o)
        {
            var count = PropertyInfoItems.Count;
            if (count > 0)
            {
                int focusedIndex = -1;
                for (int i = 0; i < count; i++)
                {
                    if (PropertyInfoItems[i].IsFocused)
                    {
                        focusedIndex = (i + 1) % count;
                        break;
                    }
                }
                if (focusedIndex == -1)
                {
                    focusedIndex = 0;
                }
                PropertyInfoItems[focusedIndex].IsFocused = true;
            }
        }

        public ICommand PrevControlCommand { get; }
        protected virtual void OnPrevControl(object o)
        {
            var count = PropertyInfoItems.Count;
            if (count > 0)
            {
                int focusedIndex = -1;
                for (int i = 0; i < count; i++)
                {
                    if (PropertyInfoItems[i].IsFocused)
                    {
                        focusedIndex = (i + (count - 1)) % count;
                        break;
                    }
                }
                if (focusedIndex == -1)
                {
                    focusedIndex = 0;
                }
                PropertyInfoItems[focusedIndex].IsFocused = true;
            }
        }

        public ObservablePreviewCollection<PropertyInfoModel> PropertyInfoItems
        {
            get
            {
                if (_propertyInfoItems is null)
                {
                    _propertyInfoItems = new ObservablePreviewCollection<PropertyInfoModel>();
                    if (_propertyInfoItems.TrackContexts[nameof(PropertyInfoModel.IsFocused)] is { } tc)
                    {
                        tc.PropertyChanged += (sender, e) =>
                        {
                            switch (e.PropertyName)
                            {
                                case nameof(ITrackContext.CurrentItems):
                                    if(tc.CurrentItems.Length == 1)
                                    {
                                        tc.CurrentItems[0].FocusEntry?.Invoke();
                                    }
                                    break;
                            }
                        };
                    }
                }
                return _propertyInfoItems;
            }
        }
        ObservablePreviewCollection<PropertyInfoModel>? _propertyInfoItems = null;

        public TrackContext<PropertyInfoModel> IsModifiedContext
        {
            get
            {
                if (_isModifiedContext is null)
                {
                    _isModifiedContext = 
                        PropertyInfoItems
                        .TrackContexts[nameof(PropertyInfoModel.IsModified)]!;
                }
                return _isModifiedContext;
            }
        }
        TrackContext<PropertyInfoModel>? _isModifiedContext = null;
        public ICommand CommandBarClickableEventCommand { get; }

        public object Item
        {
            get => _item;
            set
            {
                if( value is not null
                    && !Equals(_item, value))
                {
                    _item = value;
                    OnPropertyChanged();
                }
            }
        }
        object _item = default!;

        private void OnCommandBarClickableEvent(object o)
        {
            if (o is ClickableEventArgs e)
            {
                switch (e.EventType)
                {
                    case ClickableEventType.Clicked:
                        switch (e.OPID)
                        {
                            case ApplyCancel.Apply:
                                switch (Item)
                                {
                                    case null:
                                        break;
                                    case Type:
                                        break;
                                    default:
                                        foreach (var pii in PropertyInfoItems)
                                        {
                                            pii.Pi.SetValue(Item, pii.Value);
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                }
                if (AmbientBindingContext is IOPClickableSink sink)
                {
                    sink.SinkClickableEvent(this, e);
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
