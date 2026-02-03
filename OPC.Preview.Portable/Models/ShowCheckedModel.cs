using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OPC.Preview.Portable.Models
{
    public class ShowCheckedModel : INotifyPropertyChanged
    {
        public ShowCheckedStateGroup ShowCheckedState { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
