using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// Mutable preview event.
    /// </summary>
    /// <remarks>
    /// The returned contents of this event ARE THE AUTHORITY for this transaction.
    /// This means that the call site IS NOT.
    /// With great power comes great responsibility.
    /// </remarks>
    public class NotifyCollectionChangingEventArgs : CancelEventArgs
    {
        public NotifyCollectionChangingAction Action { get; set; }

        public IList? NewItems { get; set; }

        public IList? OldItems { get; set; }

        public int NewStartingIndex { get; set; } = -1;

        public int OldStartingIndex { get; set; } = -1;

        #region C T o r s 
        #endregion C T o r s
        protected NotifyCollectionChangingEventArgs() { }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a reset change.
        /// </summary>
        /// <param name="action">The action that caused the event (must be Reset).</param>
        /// <remarks>
        /// Since this is a preview event, the sender still holds any items that may need Dispose.
        /// </remarks>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action)
        {
            Action = action;
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event; can only be Reset, Add or Remove action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object? changedItem)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Add:
                    NewItems = new List<object?>([changedItem]);
                    break;
                case NotifyCollectionChangingAction.Remove:
                    OldItems = new List<object?>([changedItem]);
                    break;
                default:
                    this.ThrowHard<NotSupportedException>($"The {action.ToFullKey()} case is not supported.");
                    break;
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a one-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The index where the change occurred.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object? changedItem, int index)
        {
            Action = action;

            switch (action)
            {
                case NotifyCollectionChangingAction.Add:
                    NewItems = new List<object?>([changedItem]);
                    NewStartingIndex = index;
                    break;

                case NotifyCollectionChangingAction.Remove:
                    OldItems = new List<object?>([changedItem]);
                    OldStartingIndex = index;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for single-item constructor: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a multi-item change.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList? changedItems)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Add:
                    NewItems = changedItems;
                    break;

                case NotifyCollectionChangingAction.Remove:
                case NotifyCollectionChangingAction.Reset:  // Allowed for Changing only.
                    OldItems = changedItems;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for multi-item constructor: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a multi-item change (or a reset).
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="startingIndex">The index where the change occurred.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList? changedItems, int startingIndex)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Add:
                    NewItems = changedItems;
                    NewStartingIndex = startingIndex;
                    break;

                case NotifyCollectionChangingAction.Remove:
                    OldItems = changedItems;
                    OldStartingIndex = startingIndex;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for multi-item constructor with index: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object? newItem, object? oldItem)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Replace:
                    NewItems = new List<object?>([newItem]);
                    OldItems = new List<object?>([oldItem]);
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for single-item replace constructor: {action}");
            }

        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a one-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItem">The new item replacing the original item.</param>
        /// <param name="oldItem">The original item that is replaced.</param>
        /// <param name="index">The index of the item being replaced.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object? newItem, object? oldItem, int index)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Replace:
                    NewItems = new List<object?>([newItem]);
                    OldItems = new List<object?>([oldItem]);
                    NewStartingIndex = index;
                    OldStartingIndex = index;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for single-item replace constructor with index: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList newItems, IList oldItems)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Replace:
                    NewItems = newItems;
                    OldItems = oldItems;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for multi-item replace constructor: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a multi-item Replace event.
        /// </summary>
        /// <param name="action">Can only be a Replace action.</param>
        /// <param name="newItems">The new items replacing the original items.</param>
        /// <param name="oldItems">The original items that are replaced.</param>
        /// <param name="startingIndex">The starting index of the items being replaced.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList newItems, IList oldItems, int startingIndex)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Replace:
                    NewItems = newItems;
                    OldItems = oldItems;
                    NewStartingIndex = startingIndex;
                    OldStartingIndex = startingIndex;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for multi-item replace constructor with index: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a one-item Move event.
        /// </summary>
        /// <param name="action">Can only be a Move action.</param>
        /// <param name="changedItem">The item affected by the change.</param>
        /// <param name="index">The new index for the changed item.</param>
        /// <param name="oldIndex">The old index for the changed item.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object? changedItem, int index, int oldIndex)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Move:
                    NewItems = new List<object?>([changedItem]);
                    OldItems = new List<object?>([changedItem]);
                    NewStartingIndex = index;
                    OldStartingIndex = oldIndex;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for single-item move constructor: {action}");
            }
        }

        /// <summary>
        /// Construct a NotifyCollectionChangingEventArgs that describes a multi-item Move event.
        /// </summary>
        /// <param name="action">The action that caused the event.</param>
        /// <param name="changedItems">The items affected by the change.</param>
        /// <param name="index">The new index for the changed items.</param>
        /// <param name="oldIndex">The old index for the changed items.</param>
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList? changedItems, int index, int oldIndex)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangingAction.Move:
                    NewItems = changedItems;
                    OldItems = changedItems;
                    NewStartingIndex = index;
                    OldStartingIndex = oldIndex;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported action for multi-item move constructor: {action}");
            }
        }

        public NotifyCollectionChangedEventArgs CopyToChangedEvent()
        {
            var action = Action.AsEnumType<NotifyCollectionChangedAction>();

            IList? newItems = NewItems;

            // Old items are allowed in the preview event, but not in the final changed event.
            IList? oldItems = 
                action == NotifyCollectionChangedAction.Reset 
                ? null
                : OldItems;

            int newCount = newItems?.Count ?? 0;
            int oldCount = oldItems?.Count ?? 0;

            return action switch
            {
                NotifyCollectionChangedAction.Reset
                    => new NotifyPreviewCollectionChangedEventArgs(action) { OldItems = this.OldItems },

                // ADD
                NotifyCollectionChangedAction.Add when newCount == 1
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        changedItem: newItems![0],
                        index: NewStartingIndex),

                NotifyCollectionChangedAction.Add
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        changedItems: newItems,
                        startingIndex: NewStartingIndex),

                // REMOVE
                NotifyCollectionChangedAction.Remove when oldCount == 1
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        changedItem: oldItems![0],
                        index: OldStartingIndex),

                NotifyCollectionChangedAction.Remove
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        changedItems: oldItems,
                        startingIndex: OldStartingIndex),

                // REPLACE (one or many)
                NotifyCollectionChangedAction.Replace when newCount == 1 && oldCount == 1
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        newItem: newItems![0],
                        oldItem: oldItems![0],
                        index: NewStartingIndex),

                NotifyCollectionChangedAction.Replace
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        newItems: newItems!,
                        oldItems: oldItems!,
                        startingIndex: NewStartingIndex),

                // MOVE (one or many)
                NotifyCollectionChangedAction.Move when newCount == 1
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        changedItem: newItems![0],
                        index: NewStartingIndex,
                        oldIndex: OldStartingIndex),

                NotifyCollectionChangedAction.Move
                    => new NotifyPreviewCollectionChangedEventArgs(
                        action,
                        changedItems: newItems,
                        index: NewStartingIndex,
                        oldIndex: OldStartingIndex),

                _ => throw new InvalidOperationException(
                    $"Unsupported action '{action}'.")
            };
        }

        /// <summary>
        /// Returns a DictionaryEntryPreview if there is one, otherwise the raw value.
        /// </summary>
        public object? GetNewItemSingle()
        {
            if (NewItems?.Count == 1)
            {
                object? preview;
                if (NewItems[0] is DictionaryEntryPreview entry)
                {
                    preview = entry;
                }
                else
                {
                    preview = NewItems[0];
                }
                return preview;
            }
            else
            {
                this.ThrowHard<InvalidOperationException>($"Invalid 'Single' contract N = {(NewItems?.Count.ToString() ?? "null")}");
                return default;
            }
        }

        public TValue? GetNewItemSingle<TValue>()
        {
            if(NewItems?.Count == 1)
            {
                TValue? preview;
                if(NewItems[0] is DictionaryEntryPreview entry)
                {
                    preview = entry.Value.SafeAs<TValue>();
                }
                else
                {
                    preview = NewItems[0].SafeAs<TValue>();
                }
                return preview;
            }
            else
            {
                this.ThrowHard<InvalidOperationException>($"Invalid 'Single' contract N = {(NewItems?.Count.ToString() ?? "null")}");
                return default;
            }
        }        

        /// <summary>
        /// Returns a DictionaryEntryPreview if there is one, otherwise the raw value.
        /// </summary>
        public object? GetOldItemSingle()
        {
            if (OldItems?.Count == 1)
            {
                object? preview;
                if (OldItems[0] is DictionaryEntryPreview entry)
                {
                    preview = entry;
                }
                else
                {
                    preview = OldItems[0];
                }
                return preview;
            }
            else
            {
                this.ThrowHard<InvalidOperationException>($"Invalid 'Single' contract N = {(OldItems?.Count.ToString() ?? "null")}");
                return default;
            }
        }

        public TValue? GetOldItemSingle<TValue>()
        {
            if(OldItems?.Count == 1)
            {
                TValue? preview;
                if(OldItems[0] is DictionaryEntryPreview entry)
                {
                    preview = entry.Value.SafeAs<TValue>();
                }
                else
                {
                    preview = OldItems[0].SafeAs<TValue>();
                }
                return preview;
            }
            else
            {
                this.ThrowHard<InvalidOperationException>($"Invalid 'Single' contract N = {(OldItems?.Count.ToString() ?? "null")}");
                return default;
            }
        }
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"Action={Action.ToFullKey()}");
            builder.Append($", NewItems={NewItems?.Count.ToString() ?? "null"}");
            builder.Append($", OldItems={OldItems?.Count.ToString() ?? "null"}");
            builder.Append($", NewStartingIndex={NewStartingIndex}");
            builder.Append($", OldStartingIndex={OldStartingIndex}");
            return builder.ToString();
        }

        public new bool Cancel
        {
            get => base.Cancel;
            set
            {
                if (!Equals(base.Cancel, value))
                {
                    base.Cancel = value;
                }
            }
        }

        private int _appliedChanges = 0;
        public int GetAppliedChangesCount() => _appliedChanges;
        internal void SetAppliedChangesCount(int count) => _appliedChanges = count;
    }
    public static partial class CollectionExtensions
    {
        public static string ToString(this NotifyCollectionChangedEventArgs @this, bool showCounts)
        {
            if(showCounts)
            {
                var builder = new StringBuilder();
                builder.Append($"Action={@this.Action.ToFullKey()}");
                builder.Append($", NewItems={@this.NewItems?.Count.ToString() ?? "null"}");
                builder.Append($", OldItems={@this.OldItems?.Count.ToString() ?? "null"}");
                builder.Append($", NewStartingIndex={@this.NewStartingIndex}");
                builder.Append($", OldStartingIndex={@this.OldStartingIndex}");
                return builder.ToString();
            }
            else
            {
                return @this.ToString() ?? string.Empty;
            }
        }
    }
}
