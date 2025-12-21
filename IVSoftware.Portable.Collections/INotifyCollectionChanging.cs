namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// Represents the method that will handle a collection changing event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data that describes the pending collection change.</param>
    public delegate void NotifyCollectionChangingEventHandler(
        object? sender,
        NotifyCollectionChangingEventArgs e
    );

    /// <summary>
    /// Provides a mechanism for notifying listeners that a collection is about to change.
    /// </summary>
    public interface INotifyCollectionChanging
    {
        /// <summary>
        /// Occurs when the contents of the collection are about to change.
        /// </summary>
        event NotifyCollectionChangingEventHandler? CollectionChanging;
    }
}