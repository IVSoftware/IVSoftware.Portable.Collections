namespace IVSoftware.Portable.Collections
{
    using System.Collections;
    using System.Collections.Specialized;

    public sealed class NotifyPreviewCollectionChangedEventArgs : NotifyCollectionChangedEventArgs
    {
        // Note: Some new properties simply maintain the json signature in the same sequence.

        [Probationary("Supports extended action flags.")]
        public new NotifyCollectionChangedAction Action { get; }
        public new IList? NewItems => base.NewItems;
        /// <summary>
        /// The old items affected by the change (for Replace or Reset events).
        /// </summary>
        public new IList? OldItems
        {
            get => _resetItems ?? base.OldItems;
            internal set
            {
                if(Action == NotifyCollectionChangedAction.Reset) _resetItems = value;
            }
        }
        IList? _resetItems;
        public new int NewStartingIndex => base.NewStartingIndex;
        public new int OldStartingIndex => base.OldStartingIndex;

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a reset change.
        /// </summary>
        /// <param name="action">The action that caused the event (must be Reset).</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action)
            : base(action)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event; can only be Reset, Add or Remove action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem)
            : base(action, changedItem)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The index where the change occurred.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index)
            : base(action, changedItem, index)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems)
            : base(action, changedItems)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change (or a reset).
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="startingIndex">The index where the change occurred.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems, int startingIndex)
            : base(action, changedItems, startingIndex)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem)
            : base(action, newItem, oldItem)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        /// <param name="index">The index of the item being replaced.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index)
            : base(action, newItem, oldItem, index)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
            : base(action, newItems, oldItems)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        /// <param name="startingIndex">The starting index of the items being replaced.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex)
            : base(action, newItems, oldItems, startingIndex)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Move event.
        /// </summary>
        /// <param name="action">Can only be a Move action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The new index for the changed item.</param>
        /// <param name="oldIndex">The old index for the changed item.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index, int oldIndex)
            : base(action, changedItem, index, oldIndex)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Move event.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="index">The new index for the changed items.</param>
        /// <param name="oldIndex">The old index for the changed items.</param>
        public NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems, int index, int oldIndex)
            : base(action, changedItems, index, oldIndex)
        {
            Action = action;
        }
    }
}
