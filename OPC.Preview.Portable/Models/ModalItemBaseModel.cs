using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OPC.Preview.Portable.Models
{
    public class ModalItemBaseModel : INotifyPropertyChanged
    {
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, this))
            {
                PropertyChanged?.Invoke(sender, e);
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
