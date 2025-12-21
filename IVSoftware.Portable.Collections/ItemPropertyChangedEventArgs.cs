using System.ComponentModel;

namespace IVSoftware.Portable.Collections
{
    public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public ItemPropertyChangedEventArgs(object? item, PropertyChangedEventArgs e) 
            : base(e.PropertyName)
        {
            Item = item;
            ItemEvent = e;
        }
        public object? Item { get; }
        public PropertyChangedEventArgs ItemEvent { get; }
    }
}
