using System.ComponentModel;

namespace IVSoftware.Portable.Collections
{

    public class ItemPropertyChangingEventArgs : PropertyChangingEventArgs
    {
        public ItemPropertyChangingEventArgs(object? item, PropertyChangingEventArgs e)
            : base(e.PropertyName)
        {
            Item = item;
            ItemEvent = e;
        }
        public object? Item { get; }
        public PropertyChangingEventArgs ItemEvent { get; }
    }
}
