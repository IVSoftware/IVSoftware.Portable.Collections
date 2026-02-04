using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OPC.Preview.Portable.Models
{
    public class PropertyInfoModel
        : INotifyPropertyChanged
        , IOPAmbientBindingContext
    {
        public PropertyInfoModel(string key, PropertyInfo pi)
        {
            Key = key;
            Pi = pi;
        }
        public string Key { get; }
        public PropertyInfo Pi { get; }
        public object? Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged();
                    IsModified = true;
                }
            }
        }
        object? _value = string.Empty;

        [Track(TrackMode.Multiple, WherePredicate.IsTrue)]
        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (!Equals(_isModified, value))
                {
                    _isModified = value;
                    OnPropertyChanged();
                }
            }
        }

        bool _isModified = false;


        [Track(TrackMode.Single, WherePredicate.IsTrue)]
        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (!Equals(_isFocused, value))
                {
                    _isFocused = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isFocused = false;


        /// <summary>
        /// The only reason for the backing store is the race
        /// condition between Focus and IsFirst assignment.
        /// </summary>
        public bool IsFirst
        {
            get => _isFirst;
            set
            {
                _isFirst = value;
                if(_isFirst) FocusEntry?.Invoke();
            }
        }
        bool _isFirst = false;

        public Action? FocusEntry
        {
            get => _focus;
            set
            {
                if (value is not null && _focus is null)
                {
                    _focus = value;
                    if(IsFirst)
                    {
                        _focus?.Invoke();
                    }
                }
            }
        }
        Action? _focus = default;

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

        private void OnAmbientBindingContextChanged()
        {
            if (AmbientBindingContext is IOPItemEditor editor)
            {   /* G T K */
            }
        }

        object? _ambientBindingContext = default;


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
