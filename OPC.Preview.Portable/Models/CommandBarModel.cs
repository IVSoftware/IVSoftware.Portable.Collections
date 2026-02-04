using IVSoftware.Portable.Collections.Lists;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OPC.Preview.Portable.Models
{
    public class CommandBarModel
        : INotifyPropertyChanged
        , IOPAmbientBindingContext
    {
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


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
