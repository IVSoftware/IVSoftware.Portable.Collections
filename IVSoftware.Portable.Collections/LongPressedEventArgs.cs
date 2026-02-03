namespace IVSoftware.Portable.Collections
{
    public class LongPressedEventArgs : EventArgs
    {
        public LongPressedEventArgs(object? item)
        {
            Item = item;
        }
        public object? Item { get; }
    }
}
