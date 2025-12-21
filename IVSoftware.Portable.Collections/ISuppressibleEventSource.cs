namespace IVSoftware.Portable.Collections
{
    public enum SuppressionFlag
    {
        PropertyChanging = 0x1,
        PropertyChanged = PropertyChanging << 1,
        CollectionChanging = PropertyChanged << 1,
        CollectionChanged = CollectionChanging << 1,
        PropertyChanges = PropertyChanging | PropertyChanged,
        CollectionChanges = CollectionChanging | CollectionChanged,
        All = PropertyChanges | CollectionChanges
    }
    public interface ISuppressibleEventSource
    {
        IDisposable Suppress(SuppressionFlag flags = SuppressionFlag.All);

        SuppressionFlag Suppressed { get; }

        public event EventHandler EventSuppressed;
    }
}
