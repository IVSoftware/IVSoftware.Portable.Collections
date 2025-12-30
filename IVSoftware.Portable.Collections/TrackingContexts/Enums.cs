using IVSoftware.Portable.SQLiteMarkdown;

namespace IVSoftware.Portable.Collections.TrackingContexts
{
    /// <summary>
    /// Specifies the context state of an item in a CollectionView. 
    /// </summary>
    /// These values track IVSoftware.Portable.SQLiteMarkdown.ItemSelection
    /// and this enum is freely interopable though casting.
    /// </remarks>
    [Flags]
    public enum TrackState
    {
        /// <summary>
        /// The item is not selected.
        /// </summary>
        None = ItemSelection.None,

        /// <summary>
        /// The item is the only selection.
        /// This state cannot coexist with other states.
        /// </summary>
        Exclusive = ItemSelection.Exclusive,

        /// <summary>
        /// The item is one of multiple selected items.
        /// </summary>
        Multi = ItemSelection.Multi,

        /// <summary>
        /// The item is the most recently selected and is always part of a multi-selection.
        /// </summary>
        Primary = ItemSelection.Primary,
    }

    /// <summary>
    /// Mode capability.
    /// </summary>
    /// <remarks>
    /// These values track Microsoft.Maui.Controls.SelectionMode
    /// and this enum is freely interopable though casting.
    /// </remarks>
    public enum TrackMode
    {
        /// <summary>
        /// Selection not allowed.
        /// </summary>
        None,

        /// <summary>
        /// One-hot selection.
        /// </summary>
        Single,

        /// <summary>
        /// Multiple selection allowed.
        /// </summary>
        /// <remarks>
        /// This mode is available as a temporary elevation by 
        /// answering the ModifierRequest event with string[]
        /// containing "control", "shift", and/or "alt".
        /// </remarks>
        Multiple
    }
    enum TrackValueDomain
    {
        Incompatible,
        Binary,
        Stateful
    }
}
