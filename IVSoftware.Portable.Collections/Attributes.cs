using IVSoftware.Portable.Collections.Dictionaries;

namespace IVSoftware.Portable
{
    #region I V S    C A N O N I C A L 
    [Canonical("Source for this library and other IVS NuGets")]
    public class CanonicalAttribute : Attribute
    {
        public CanonicalAttribute(string? canon = null)
        {
            Canon = canon ?? string.Empty;
        }
        public string Canon { get; }
    }

    public class CarefulAttribute : Attribute
    {
        public CarefulAttribute(string? ofWhat = null)
        {
            OfWhat = ofWhat ?? string.Empty;
        }

        public string OfWhat { get; }
    }

    public class ProbationaryAttribute : Attribute
    {
        public ProbationaryAttribute(string? reason = null)
        {
            Reason = reason ?? string.Empty;
        }

        public string Reason { get; }
    }

    public class ScaffoldingAttribute : Attribute
    {
    }

    public class UnsupportedAttribute : Attribute
    {
    }

    /// <summary>
    /// This exists to make arbitrary indexer overloads easier to locate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexerAttribute : Attribute
    {
        public IndexerAttribute(string? description = null)
        {
            if(description is not null)
            {
                Description = description;
            }
        }
        public IndexerAttribute(Type tKey, Type tValue) 
        {
            TKey = tKey;
            TValue = tValue;
        }
        public Type? TKey { get; }
        public Type? TValue { get; }
        public string Description { get; } = string.Empty;
    }
    #endregion I V S    C A N O N I C A L

    namespace Collections
    {
        /// <summary>
        /// Reflection base for all attributes in this NuGet.
        /// </summary>
        public abstract class CollectionAttribute : Attribute { }

        public abstract class KeySegmentAttribute : CollectionAttribute
        {
            /// <summary>
            /// So-named because these elements are *used like* objects, 
            /// not because they actually are. It's a white lie that
            /// keeps the MakePathFromObjects semantics intutive.
            /// </summary>
            public string[] KeyChainObjects { get; init; } = [];
        }

        /// <summary>
        /// A key segment that begins with its own type name.
        /// </summary>
        [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
        public class AbsoluteKeySegmentAttribute : KeySegmentAttribute
        {
            public AbsoluteKeySegmentAttribute() { }
            public AbsoluteKeySegmentAttribute(string @string, params string[] moreStrings)
            {
                KeyChainObjects =
                    @string
                    .UnrollCsvArgs(moreStrings);
            }
            public AbsoluteKeySegmentAttribute(Type type, params Type[] moreTypes)
            {
                KeyChainObjects = 
                    type
                    .UnrollKeyChainObjects(moreTypes)
                    .Cast<Type>()
                    .Select(_=>_.Name)
                    .ToArray();
            }
        }

        /// <summary>
        /// A key segment that begins with its member name (and skips the enum type name).
        /// </summary>
        [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = false)]
        public class RelativeKeySegmentAttribute : KeySegmentAttribute
        {
            public RelativeKeySegmentAttribute() { }
            public RelativeKeySegmentAttribute(string @string, params string[] moreStrings)
            {
                KeyChainObjects =
                    @string
                    .UnrollCsvArgs(moreStrings);
            }
            public RelativeKeySegmentAttribute(Type type, params Type[] moreTypes)
            {
                KeyChainObjects =
                    type
                    .UnrollKeyChainObjects(moreTypes)
                    .Cast<Type>()
                    .Select(_ => _.Name)
                    .ToArray();
            }
        }

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        public class StrongTypedDictionaryAttribute : Attribute
        {
            public StrongTypedDictionaryAttribute(Type tKey, Type tValue)
            {
                TKey = tKey;
                TValue = tValue;
            }
            public Type TKey { get; }
            public Type TValue { get; }
        }

        /// <summary>
        /// Declares an interface as a unilateral contract that may activate
        /// an alternate runtime type when compatibility is detected.
        /// </summary> 
        /// <remarks>
        /// Unilateral contracts are resolved via runtime reflection.
        /// For scenarios where contract activation occurs frequently,
        /// BriskDictionary provides an efficient reflection caching
        /// substrate and is a natural companion to this pattern.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
        public class UnilateralContractAttribute : CollectionAttribute
        {
            public UnilateralContractAttribute(
                Type activateAs,
                params string[] knownCompatibleTypes)
            {
                ActivateAsType = activateAs;
                KnownCompatibleTypes = knownCompatibleTypes;
            }
            public Type ActivateAsType { get; }

            /// <summary>
            /// These types might not be known at compile time
            /// but will be recognized if they present at runtime.
            /// </summary>
            public string[] KnownCompatibleTypes { get; }
        }

        /// <summary>
        /// Values associated with the ActionContractAttribute,
        /// </summary>
        [Flags]
        public enum ActionContract 
        { 
            /// <summary>
            /// This event stores value-only objects.
            /// </summary>
            Value = 0x0,

            /// <summary>
            /// This event stores key-value pairs and this was explicitly set by the [ActionContractAttribute].
            /// </summary>
            DictionaryEntry = 0x1,

            /// <summary>
            /// This event stores key-value pairs but we only know that because it tried to store one.
            /// </summary>
            /// <remarks>
            /// There is little danger in this and it only matters when the 
            /// initializing new or old values contains explicitly null objects.
            /// But why take chances? Go back and set the [ActionContractAttribute] on your enum!
            /// </remarks>
            DictionaryEntryDetected = 0x3,
        }


        /// <summary>
        /// Allows the framework can automatically select between <see cref="CoercibleValuePreview"/>
        /// and <see cref="CoercibleDictionaryEntryPreview"/> by inspecting this attribute.
        /// </summary>
        [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
        public class ActionContractAttribute : CollectionAttribute
        {
            public ActionContractAttribute() { }
            public ActionContractAttribute(ActionContract actionContract) { }
            public ActionContract ActionContract { get; set; } = ActionContract.Value;
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        public sealed class RequiresAttribute : CollectionAttribute
        {
            public RequiresAttribute(Type type, params Type[] moreTypes) 
            {
                Availability = new[] { type }.Concat(moreTypes).ToArray(); 
            }
            public Type[] Availability { get; }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        public sealed class ReturnsAttribute : CollectionAttribute
        {
            public ReturnsAttribute(Type type) 
            {
                Returns = type;
            }
            public Type? Returns { get; } = null;
        }
    }
}