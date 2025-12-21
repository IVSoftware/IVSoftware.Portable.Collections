using System.Xml;

namespace IVSoftware.Portable.Collections
{
    public enum StdCollectionXElement
    {
        /// <summary>
        /// Indicates the static framework model for this NuGet bundle.
        /// </summary>
        model,

        /// <summary>
        /// An XElement bound to an unknown IDictionary 
        /// </summary>
        xdunk,

        /// <summary>
        /// In a model report, indicates a temporary section where
        /// the dictionary values are visible by key-value
        /// </summary>
        values,

        /// <summary>
        /// In a model report, indicates that an ephemeral DictionaryEntry will follow.
        /// </summary>
        entry,

        /// <summary>
        ///  The Key portion of the DictionaryEntry
        /// </summary>
        key,

        /// <summary>
        ///  The Value portion of the DictionaryEntry
        /// </summary>
        value,
    }

    public enum StdCollectionXAttribute
    {
        dunk,

        key,

        returns,

        count,

        type,
    }

    [Flags]
    public enum FormattedTypeNameOptionFlag
    {
        UseShortTypeName = 1 << 0,
    }

    [Flags]
    public enum FormattedDictNameOptionFlag
    {
        Personality = 0x01,
        Count = Personality << 1,
        All = Personality | Count,
    }

    /// <summary>
    /// Represents the possible outcomes of a type change attempt.
    /// </summary>
    public enum StrongTypesUpgradeStatus
    {
        /// <summary>
        /// The dictionary was already correctly typed, so no change was required.
        /// </summary>
        NoChangeNeeded,

        /// <summary>
        /// The type change completed successfully and the dictionary was upgraded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// The type change failed because the TKey is fundamentally incompatible.
        /// </summary>
        IncompatibleTKey,

        /// <summary>
        /// The type change failed because one or more values were incompatible.
        /// </summary>
        IncompatibleTValue,

        /// <summary>
        /// The type change failed because the IDictionary could not be cast to BriskDictionaryWrapper
        /// </summary>
        NotUpgradable,

        /// <summary>
        /// The type change failed because the IDictionary is not generic
        /// </summary>
        RequestedModeIsNotStrongTyped,
    }

    [Flags]
    public enum ModifiedFlag
    {
        NoChanges = 0x0000,
        Action = 0x0001,
        NewItemsList = Action << 1,
        NewKeyCoerced = NewItemsList << 1,
        NewValueCoerced = NewKeyCoerced << 1,
        NewIndexCoerced = NewValueCoerced << 1,
        NewPropertyChanged = NewIndexCoerced << 1,
        NewCollectionChanged = NewPropertyChanged << 1,
        OldItemsList = 0x0100,
        OldKeyCoerced = OldItemsList << 1,
        OldValueCoerced = OldKeyCoerced << 1,
        OldIndexCoerced = OldValueCoerced << 1,
        OldPropertyChanged = OldIndexCoerced << 1,
        OldCollectionChanged = OldPropertyChanged << 1,
    }

    public enum StatusAsList
    {
        /// <summary>
        /// The receiver is null.
        /// </summary>
        Null = 0,

        /// <summary>
        /// Heuristic Count property returns Zero.
        /// </summary>
        Empty,

        /// <summary>
        /// Heuristic Count property returns One.
        /// </summary>
        Single,

        /// <summary>
        /// Heuristic Count property returns a value greater than one.
        /// </summary>
        Multi,

        /// <summary>
        /// All efforts to obtain a Count from this object have failed.
        /// </summary>
        NotSupported,
    }


#if false

    public enum BDITraverseMode
    {
        FindOrCreate,
        FindOrFalse,
        FindOrThrow,
    }

    [AbsoluteKeySegment]
    public enum StdNativeBriskCache
    {
        /// <summary>
        /// OF COURSE we know that the plural 
        /// of 'alias' is 'aliases' not 'alii'. 
        /// WHAT'S YOUR POINT?
        /// </summary>
        Alii,
    }

    /// <summary>
    /// Option flag for <see cref="BriskComplexKey.UnrollKeyChain(object, object[])"/> behaviors.
    /// </summary>
    /// <remarks>
    /// The <see cref="BriskComplexKey.DefaultRootKeyBehavior"/> setting can be ephemerally overridden
    /// by supplying this flag as the first argument of the unroll.
    /// <para/>
    /// Example (Default): Produces "[ref: ExampleClass]"
    /// <code>
    /// var o = new ExampleClass();
    /// var passKey = new BSignature(o); // Call unroll via the ctor then inspect ToString().
    /// </code>
    /// <para/>
    /// Example (Override): Produces "[type: ExampleClass]"
    /// <code>
    /// var o = new ExampleClass();
    /// var passKey = new BSignature(RootKeyBehavior.GetTypeAuto, o);
    /// </code>
    /// </remarks>
    public enum RootKeyBehavior
    {
        /// <summary>
        /// Default unless overridden by BriskSignature.DefaultRootKeyBehavior
        /// </summary>
        /// <remarks>
        /// Useful e.g. when the signature is used for instance-specific lookups.
        /// Example (ByRef) : PropertyInfo pi = brisk[btn, typeof(Settings)][nameof(Setting.Indentation)]
        /// Example (ByType): PropertyInfo pi = brisk[btn.GetType(), typeof(PropertyInfo)][nameof(Button.IsVisible)]
        /// </remarks>
        Verbatim,

        /// <summary>
        /// Automatically convert references using GetType().
        /// </summary>
        /// <remarks>
        /// Useful e.g. when the signature is used primarily for caching.
        /// Example (ByType): PropertyInfo pi = brisk[btn, typeof(PropertyInfo)][nameof(Button.IsVisible)]
        /// </remarks>
        GetType,
    }

    /// <summary>
    /// Defines how a dictionary responds when a requested key is missing.
    /// </summary>
    /// <remarks>
    /// Distinguishes tolerant from insistent behavior and whether a missing key
    /// results in mutation or ephemeral return. Used by dictionary patterns to
    /// standardize lookup and creation rules.
    /// </remarks>
    [Flags]
    public enum MissingKeyRule
    {
        /// <summary>
        /// Missing key is accepted and returns null.
        /// </summary>
        /// <remarks>
        /// The lookup produces a null result without altering the dictionary.
        /// Equivalent to silent acceptance of absence.
        /// </remarks>
        TolerateAndReturnNull,

        /// <summary>
        /// Missing key is accepted and added with a null value.
        /// </summary>
        /// <remarks>
        /// The dictionary is updated to include the missing key, explicitly storing
        /// a null value. Signals intentional tolerance of absence.
        /// </remarks>
        TolerateAndAddKeyWithNullValue,

        /// <summary>
        /// Missing key is not accepted and adds a new instance.
        /// </summary>
        /// <remarks>
        /// 1. Run heuristics to create a new instance
        /// 2. Notify EUD by raising event with proposal (noting that heuristic may not have produced a viable object).
        /// 3. Return the result to the dictionary and 'ThrowHard' on failure.
        /// </remarks>
        InsistAndReturnNew,

        /// <summary>
        /// Missing key is not accepted and adds a new instance.
        /// </summary>
        /// <remarks>
        /// 1. Run heuristics to create a new instance
        /// 2. Notify EUD by raising event with proposal (noting that heuristic may not have produced a viable object).
        /// 3. Add the result to the dictionary and 'ThrowHard' on failure.
        /// </remarks>
        InsistAndAddKeyWithReturnNew,
    }
    /// <summary>
    /// Describes the state and intent of a missing key proposal.
    /// </summary>
    /// <remarks>
    /// Combines tolerance, initialization, and success flags for event-driven
    /// key creation. Serves as a call-and-response payload between dictionary
    /// and listener, allowing handlers to infer both policy and outcome.
    /// </remarks>
    [Flags]
    public enum ArbitrationProposal
    {
        #region Is the personality (default) Tolerant or is it set to Insistent?
        /// <summary>
        /// Indicates tolerant behavior (missing key is acceptable).
        /// </summary>
        // [Reserved]
        // Tolerate = 0x00,

        /// <summary>
        /// Indicates insistent behavior (missing key must be resolved).
        /// </summary>
        Insist = 0x01,
        #endregion


        #region When to we make an entry for requested key? (default) only when it succeeds or write null value to key to persist the attempt.
        /// <summary>
        /// Entry is added only on successful initialization.
        /// </summary>
        // [Reserved]
        // EnsureEntryOnSuccess = 0x00,

        /// <summary>
        /// Entry is always added, even if initialization fails or yields null.
        /// </summary>
        EnsureEntryAlways = Insist << 1,
        #endregion

        #region Did a ctor fault while activating?
        // Possible duplicate of ValueIsAssignableTo = 0x40,
        /// <summary>
        /// Initialization not attempted or succeeded.
        /// </summary>
        // [Reserved]
        // NoFaultOrNoActivation = 0x00,

        /// <summary>
        /// Initialization attempted and failed to produce a usable object.
        /// </summary>
        IsFaultedRunningActivation = EnsureEntryAlways << 1,
        #endregion

        #region Is the instance the result of intentional imperative or is it a guess?
        /// <summary>
        /// If succeeded, the source of the instantiated object
        /// </summary>
        // [Reserved]
        // IsObjectHeuristic = 0x00,

        /// <summary>
        /// Object was supplied explicitly by the handler.
        /// </summary>
        IsObjectExplicit = IsFaultedRunningActivation << 1,
        #endregion

        #region Identify where the setting took place

        /// <summary>
        /// ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // IsUnalteredByHandler = 0x00,

        /// <summary>
        /// Object was supplied explicitly by the handler. ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // IsSetByHandler = IsObjectExplicit << 1,
        #endregion


        #region Arbitration resulted in an assignable value
        /// <summary>
        /// ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // ValueCannotBeAssignedTo= 0x00,

        /// <summary>
        /// Object was supplied explicitly by the handler. ALWAYS 0 in proposal.
        /// </summary>
        ValueIsAssignableTo = IsObjectExplicit << 2,
        #endregion

        #region Interop success
        /// <summary>
        /// Type Exchange Abstraction (TEA) is valid for object.
        /// </summary>
        // [Reserved]
        // ValueIsInteroperableWith = 0x80,
        #endregion
    }

    /// <summary>
    /// The handler RECEIVES A COPY of the proposal and may SET OR CLEAR individual flags.
    /// </summary>
    [Flags]
    public enum ArbitrationCounterOffer
    {
        /// <summary>
        /// Indicates tolerant behavior (missing key is acceptable).
        /// </summary>
        // [Reserved]
        // Tolerate = 0x00,

        /// <summary>
        /// Indicates insistent behavior (missing key must be resolved).
        /// </summary>
        Insist = ArbitrationProposal.Insist,


        /// <summary>
        /// Entry is added only on successful initialization.
        /// </summary>
        // [Reserved]
        // EnsureEntryOnSuccess = 0x00,

        /// <summary>
        /// Entry is always added, even if initialization fails or yields null.
        /// </summary>
        EnsureEntryAlways = ArbitrationProposal.EnsureEntryAlways,


        #region Did a ctor fault while activating?
        // Possible duplicate of ValueIsAssignableTo = 0x40,
        /// <summary>
        /// Initialization not attempted or succeeded.
        /// </summary>
        // [Reserved]
        // NoFaultOrNoActivation = 0x00,

        /// <summary>
        /// Initialization attempted and failed to produce a usable object.
        /// </summary>
        IsFaultedRunningActivation = ArbitrationProposal.IsFaultedRunningActivation,
        #endregion


        /// <summary>
        /// If succeeded, the source of the instantiated object
        /// </summary>
        // [Reserved]
        // IsObjectHeuristic = 0x00,

        /// <summary>
        /// Object was supplied explicitly by the handler.
        /// </summary>
        IsObjectExplicit = ArbitrationProposal.IsObjectExplicit,


        /// <summary>
        /// ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // IsUnalteredByHandler = 0x00,

        /// <summary>
        /// Object was supplied explicitly by the handler. ALWAYS 0 in proposal.
        /// </summary>
        IsSetByHandler = IsObjectExplicit << 1,

        /// <summary>
        /// ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // ValueCannotBeAssignedTo= 0x00,

        /// <summary>
        /// Object was supplied explicitly by the handler. ALWAYS 0 in proposal.
        /// </summary>
        ValueIsAssignableTo = ArbitrationProposal.ValueIsAssignableTo,

        /// <summary>
        /// ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // ValueIsNotInteroperableWith = 0x00,

        /// <summary>
        /// Type Exchange Abstraction (TEA) is valid for object.
        /// </summary>
        ValueIsInteroperableWith = ValueIsAssignableTo << 1,

        /// <summary>
        /// ALWAYS 0 in proposal.
        /// </summary>
        // [Reserved]
        // DoNotCancelArbitration = 0x00,

        /// <summary>
        /// Request to cancel arbitration entirely.
        /// </summary>
        CancelArbitration = 0x8000,
    }

    /// <summary>
    /// Controls indentation style applied during JSON serialization.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="Formatting"/> with flags that distinguish outer-level and nested
    /// indentation scopes. Use <see cref="IndentOuter"/> for top-level readability, or
    /// <see cref="IndentInner"/> to include recursive indentation for embedded JSON blocks.
    /// </remarks>
    [Flags]
    public enum JsonFormatting
    {
        /// <summary>
        /// No special formatting is applied. This is the default.
        /// </summary>
        None = Formatting.None,

        /// <summary>
        /// Applies standard indentation to the outermost JSON structure.
        /// </summary>
        IndentOuter = Formatting.Indented,

        /// <summary>
        /// Applies recursive indentation to nested JSON blocks within composite documents.
        /// </summary>
        IndentInner = IndentOuter | (IndentOuter << 1),
    }
    [Flags]
    public enum JsonFormattingEx : ushort
    {
        /// <summary>
        /// No special formatting is applied. This is the default.
        /// </summary>
        None = JsonFormatting.None,

        /// <summary>
        /// Applies standard indentation to the outermost JSON structure.
        /// </summary>
        IndentOuter = JsonFormatting.IndentOuter,

        /// <summary>
        /// Applies recursive indentation to nested JSON blocks within composite documents.
        /// </summary>
        IndentInner = JsonFormatting.IndentInner,

        /// <summary>
        /// Find the .NET BCL NotifyCollectionChangedEvent 
        /// superclass and serialize its non-virtual members.
        /// </summary>
        UseBCL = IndentInner << 1,

        /// <summary>
        /// The gold standard for BCL in tests.
        /// </summary>
        UseBclDefault = UseBCL | IndentOuter,
    }

    /// <summary>
    /// Describes the interoperablity of a
    /// typeFrom WRT a typeTo
    /// </summary>
    public enum TransferMode
    {
        /// <summary>
        /// Objects are the same type and do not require interop.
        /// </summary>
        DirectTransfer = 1,

        /// <summary>
        /// Convertible value types that do not require custom conversion.
        /// </summary>
        ValueConvert,

        /// <summary>
        /// Returns a defined ITypeExchangeAbstraction, either by 
        /// constructing new or by retrieving cache for the instance.
        /// </summary>
        ToTEA,


        /// <summary>
        /// Calls AsT(object) non-extension on the interop.
        /// </summary>
        FromTEA,

        /// <summary>
        /// Neither target is wrappes as an ITypeExchangeObject
        /// </summary>
        FindOrConcoctRecipe,
    }

    public enum StdCachePlaceholder
    {
        Type_,
    }
#endif
}
