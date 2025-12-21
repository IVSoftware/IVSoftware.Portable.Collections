using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections
{
    public class PropertyChangingPreviewEventArgs<T> : PropertyChangingEventArgs
    {
        public PropertyChangingPreviewEventArgs(
            T? oldValue,
            T? newValue,
            bool cancel = false,
            [CallerMemberName] string? propertyName = null) : base(propertyName) 
        {
            OldValue = oldValue;
            NewValue = newValue;
            Cancel = false;
        }
        public T? OldValue { get; }
        public T? NewValue { get; set; }
        public bool Cancel { get; set; }
    }
}
