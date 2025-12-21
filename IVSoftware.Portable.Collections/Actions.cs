using System.Collections.Specialized;

namespace IVSoftware.Portable.Collections
{
	[Flags, ActionContract(ActionContract = ActionContract.Value)]
	[Canonical("All actions in this package derive from this set of values.")]
	public enum NotifyExtendedCollectionChangedAction
	{
		/// <summary>
		/// Indicates an adding or added operation and can be combined with Batch.
		/// </summary>
		Add = NotifyCollectionChangedAction.Add, // Nibble 0

        /// <summary>
        /// Indicates a removing or removed operation and can be combined with Batch.
        /// </summary>
        Remove = NotifyCollectionChangedAction.Remove,

		/// <summary>
		/// Indicates a replacing or replaced operation and can be combined with Batch.
		/// </summary>
		Replace = NotifyCollectionChangedAction.Replace,

		/// <summary>
		/// Indicates a moving or moved operation and can be combined with Batch.
		/// </summary>
		Move = NotifyCollectionChangedAction.Move,

		/// <summary>
		/// Indicates a resetting or reset operation and can be combined with Batch.
		/// </summary>
		Reset = NotifyCollectionChangedAction.Reset,

        /// <summary>
        /// Signals a basic action that is being performed as a batch.
        /// </summary>
        /// <remarks>
        /// This is an ACTION not a FLAG.
        /// </remarks>        
        Batch = Reset | Replace,

		#region E X T E N D E D    R E P L A C E    F L A G S
		/// <summary>
		/// Notification that a condition is about to be tolerated.
		/// </summary>
		/// <remarks>
		/// A good example of a condition that can be tolerated is a missing dictionary key.
		/// </remarks>
		Tolerate = 0x010, // Nibble 1

        /// <summary>
        /// Notification that (typically) heuristic remedies are being employed to remedy an intolerable conditon.
        /// </summary>
        /// <remarks>
        /// A good example of an intolerable condition is when dictionary is required to return a non-null value even when a key is detected.
        /// </remarks>
        Insist = Tolerate << 1,

		/// <summary>
		/// Flag specific to Brisk Dictionary indicating that a new IDictionary is being or has been created.
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		Brisk = Insist << 1,
        #endregion E X T E N D E D    R E P L A C E    F L A G S

	
		#region E X T E N D E D    E R R O R    F L A G S
        /// <summary>
        /// Signals a distinct action that has detected a duplicate
        /// </summary>
        Duplicate = 0x100,	// Nibble 2

		/// <summary>
		/// Inset or indexer error
		/// </summary>
        IndexOutOfRange = Duplicate << 1,
        #endregion E X T E N D E D    E R R O R    F L A G S
    }

    /// <summary>
    /// Actions for collection Changed and Changing events
    /// </summary>
    [Flags, ActionContract(ActionContract = ActionContract.Value)]
    public enum NotifyCollectionChangingAction : ushort
    {
        Add = NotifyExtendedCollectionChangedAction.Add,
        Remove = NotifyExtendedCollectionChangedAction.Remove,
        Replace = NotifyExtendedCollectionChangedAction.Replace,
        Move = NotifyExtendedCollectionChangedAction.Move,
        Reset = NotifyExtendedCollectionChangedAction.Reset,
    }
}
