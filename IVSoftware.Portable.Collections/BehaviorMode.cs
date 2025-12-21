namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// Provides castable behavior modes for collections and dictionaries.
    /// </summary>
    /// <remarks>
    /// This is done so that subclasses that employ both ITolerant 
    /// and IInsistent can freely cast their modes without overlap.
    /// </remarks>
    [Canonical("Castable non-conflicting reference values for all Mode enums.")]
    public enum BehaviorMode
    {
        /// <summary>
        /// Normal behavior in every respect.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Tolerates KeyNotFound or IndexOutOfRange by returning default without raising exceptions.
        /// </summary>
        TolerantReturnDefault = Normal + 1,

        /// <summary>
        /// Tolerates KeyNotFound or IndexOutOfRange by adding a new default entry without raising exceptions.
        /// </summary>
        /// <remarks>
        /// This tolerant variant is ideal e.g. for caching "missed attempts."
        /// </remarks>
        TolerantCreateDefaultEntry = TolerantReturnDefault + 1,

        /// <summary>
        /// Insists upon returning an non-null instance of TValue with heuristic fallbacks. 
        /// Any new not-null instances thus created are then added to the collection.
        /// </summary>
        /// <remarks>
        /// Distinct from Normal (which would also throw) because a null value 
        /// can't be written and if one is somehow retrieved from an otherwise
        /// valid index, that will be a throw if not remedied also.
        /// </remarks>
        InsistentNotNull = TolerantCreateDefaultEntry + 1,
    }
}
