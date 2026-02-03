namespace OPC.Preview.Maui.Controls
{
    public class ItemClickedEventArgs : EventArgs
    {
        public ItemClickedEventArgs(object? item)
        {
            Item = item;
        }
        public object? Item { get; }
    }
}