using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static IVSoftware.Portable.Collections.CollectionExtensions;

namespace IVSoftware.Portable.Collections
{
    public static partial class CollectionExtensions
    {
        /// <summary>
        /// Produces a readable name for the specified type, expanding generic type
        /// definitions into a formatted string with argument type names included.
        /// Supports optional flags such as <see cref="FormattedTypeNameOptionFlag">
        /// </summary>
        public static string ToFormattedTypeName(
            this Type @this,
            FormattedTypeNameOptionFlag options = 0)
            => ToFormattedTypeName(@this, out _, options);

        /// <summary>
        /// Produces a readable name for the specified type, expanding generic type
        /// definitions into a formatted string with argument type names included.
        /// Supports optional flags such as <see cref="FormattedTypeNameOptionFlag">
        /// </summary>
        [Canonical("For this NuGet and others")]
        public static string ToFormattedTypeName(
            this Type @this,
            out Type[] types,
            FormattedTypeNameOptionFlag options = 0)
        {
            bool nameOnly = options.HasFlag(FormattedTypeNameOptionFlag.UseShortTypeName);

            // Handle generic case
            if (@this.IsGenericType)
            {
                var genericType = @this.GetGenericTypeDefinition();
                var genericName = nameOnly
                    ? genericType.Name
                    : genericType.FullName ?? genericType.Name;

                // Strip the arity backtick
                var unmangled = genericName.Contains('`')
                    ? genericName[..genericName.IndexOf('`')]
                    : genericName;

                types = @this.GetGenericArguments();
                var args = types.Select(t => t.ToFormattedTypeName(options));

                return $"{unmangled}<{string.Join(", ", args)}>";
            }

            // Non-generic
            types = Type.EmptyTypes;
            return nameOnly
                ? @this.Name
                : @this.FullName ?? @this.Name;
        }

        /// <summary>
        /// Attempts to activate a concrete instance for the specified contract type.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="UnilateralContractAttribute"/> metadata on <paramref name="this"/> 
        /// to locate and instantiate an implementing type with the provided arguments.
        /// This is an optional service that enables declarative binding when available, but
        /// can also be freely implemented like any other normal .NET interface as well.
        /// </remarks>
        public static bool TryActivateUnilateralContract<T>(this Type @this, out T? instance, params object[] args)
        {
            instance = default;
            if (@this is null)
            {
                // Throw interceptable exception to EUD.
                @this.ThrowSoft<ArgumentNullException>(nameof(@this));
                return false;
            }

            // Ensure that the contract type is an interface.
            if (!typeof(T).IsInterface)
            {
                @this.ThrowSoft<InvalidOperationException>(
                    $"Type '{typeof(T).FullName}' must represent an interface.");
                return false;
            }
            if (@this.GetCustomAttribute<UnilateralContractAttribute>()?.ActivateAsType is { } type)
            {
                // Ensure that the activated type is concrete.
                if (type.IsAbstract)
                {
                    @this.ThrowSoft<InvalidOperationException>(
                        $"Type '{type.FullName}' must represent a concrete class.");
                    return default!;
                }
                if (Activator.CreateInstance(type, args) is { } concrete)
                {
                    if (concrete is T assignable)
                    {
                        instance = assignable;
                        return true;
                    }

                    @this.ThrowSoft<InvalidCastException>(
                        $"Type '{type.FullName}' does not implement '{typeof(T).FullName}'.");
                    return default!;
                }
                else
                {
                    @this.ThrowSoft<NullReferenceException>(
                         $"Failed to activate contract type '{typeof(T).FullName}'.");
                    return default!;
                }
            }
            else
            {
                @this.ThrowSoft<InvalidOperationException>(
                         $"The [UnilateralContractAttribute] attribute is missing for '{typeof(T).FullName}'.");
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve a strongly-typed dictionary view from an observable dictionary.
        /// </summary>
        /// <remarks>
        /// This method performs a safe type recovery in two passes:
        /// 1. Direct cast — if the source already implements <see cref="IDictionary{TKey,TValue}"/>, 
        ///    it is returned as-is.
        /// 2. Indirect host lookup — if the source participates in a <see cref="BriskDictionaryWrapper"/> 
        ///    relationship, the wrapper is queried to produce a strongly-typed view via 
        ///    <see cref="BriskDictionaryWrapper.AsStronglyTypedDictionary{TKey,TValue}()"/>.
        /// </remarks>
        [return: NotNullIfNotNull(nameof(@this))]
        public static IDictionary<TKey, TValue>? SafeAs<TKey, TValue>(this IObservableDictionary? @this)
            where TKey : notnull
        {
            if (@this is IDictionary<TKey, TValue> valueT)
            {
                return valueT;
            }
            else if(@this.TryGetHost(out var bdw))
            {
                return bdw.@base.SafeAs<TKey, TValue>();
            }
            else
            {
                return default;
            }
        }

#if false
        [return: NotNullIfNotNull(nameof(@this))]
        public static T? AsTyped<T>(this T? @this)
            where T : IObservableDictionary
            => @this is BriskDictionaryWrapper bdw
                ? bdw.@base.SafeAs<T>()
                : @this;
#endif

        [Canonical][return: NotNullIfNotNull(nameof(@this))]
        public static T? SafeAs<T>(this object? @this) => @this.AsNotNullIfNotNull<T>();

        /// <summary>
        /// Provides a shorthand alias for <see cref="AsNotNullIfNotNull{T}(object?)"/>.
        /// </summary>
        /// <remarks>
        /// Performs a safe conditional cast that preserves nullability metadata. 
        /// Equivalent to <see cref="AsNotNullIfNotNull{T}(object?)"/>, but reads more naturally in fluent expressions.
        /// </remarks>
        [return: NotNullIfNotNull(nameof(@this))]
        public static T? AsNotNullIfNotNull<T>(this object? @this)
        {
            if (@this is T valueT)
            {
                return valueT;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Preemptive compatibility check for calls made to generic collections from non generic interfaces like IList.
        /// </summary>
        public static bool IsAssignableAs<T>(this object? @this, out T value)
        {
            if (@this is T valueT)
            {
                value = valueT;
                return true;
            }

            value = default!; // Because if T is null but not nullable the return is false.

            if (@this is null)
            {
                // Null is assignable if T is a reference type or Nullable<T>
                return !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null;
            }
            return false;
        }

        /// <summary>
        /// Preemptive compatibility check for calls made to generic collections from non generic interfaces like IList.
        /// </summary>
        public static bool IsAssignableAs(this object? @this, Type type)
        {
            if (@this is null)
            {
                // Null is assignable if type is reference type OR Nullable<T>
                return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
            }

            return @this.GetType().IsAssignableTo(type);
        }

        /// <summary>
        /// Gets the first custom attribute of the specified type applied to an enum value.
        /// </summary>
        public static TAttribute? GetCustomAttribute<TAttribute>(
            this Enum value)
            where TAttribute : Attribute
        {
            var field = value.GetType().GetField(value.ToString());
            return field?
                .GetCustomAttributes(typeof(TAttribute), false)
                .OfType<TAttribute>()
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets all custom attributes of the specified type applied to an enum value.
        /// </summary>
        public static IEnumerable<TAttribute> GetCustomAttributes<TAttribute>(
            this Enum value)
            where TAttribute : Attribute
        {
            var field = value.GetType().GetField(value.ToString());
            return field?
                .GetCustomAttributes(typeof(TAttribute), false)
                .OfType<TAttribute>()
                ?? Enumerable.Empty<TAttribute>();
        }

        /// <summary>
        /// Attempts to validate a proposed activation type for <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="type">Candidate type to validate.</param>
        /// <param name="@throw">If true, propagates @throw to ThrowSoft for advisory escalation.</param>
        /// <param name="validated">Receives the validated type on success, otherwise null.</param>
        /// <returns>True if validation succeeds; false if rejected.</returns>
        internal static bool TryValidateType<TValue>(this Type? type, out Type? validated, bool @throw)
        {
            validated = null;

            if (localIsNull())
            {
                return false;
            }

            if (localPreviewUnilateralContract() is Type uc)
            {
                validated = uc;
                return true;
            }

            if (localIsIntrinsicActivatable())
            {
                validated = type;
                return true;
            }

            if (localIsAbstract())
            {
                return false;
            }

            if (!localIsAssignable())
            {
                return false;
            }

            if (!localTryGetConstructorInfo(out _))
            {
                return false;
            }

            validated = type;
            return true;

            #region L o c a l   F x
            bool localIsNull()
            {
                if (type is null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            Type? localPreviewUnilateralContract()
            {
                if (type!.IsAbstract &&
                    type.GetCustomAttribute<UnilateralContractAttribute>()?.ActivateAsType is { } ucType)
                {
                    if (ucType.IsAbstract)
                    {
                        _ = typeof(TValue).ThrowSoft<InvalidOperationException>(
                            messageOrId: $@"Type '{ucType.FullName ?? type.Name}' is abstract and invalidates Unilateral Contract.",
                            @throw: @throw);
                    }
                    else
                    {
                        // Not circular if not abstract. GUARANTEED.
                        if (ucType.TryValidateType<TValue>(out ucType, @throw))
                        {
                            return ucType;
                        }
                    }
                }
                return null;
            }

            bool localIsAbstract()
            {
                if (type!.IsAbstract)
                {
                    _ = typeof(TValue).ThrowSoft<InvalidOperationException>(
                        messageOrId: $@"Type '{type.FullName ?? type.Name}' is abstract and cannot be used for activation.",
                        @throw: @throw);
                    return true;
                }
                return false;
            }

            bool localIsAssignable()
            {
                if (!typeof(TValue).IsAssignableFrom(type!))
                {
                    _ = typeof(TValue).ThrowSoft<InvalidCastException>(
                        messageOrId: $@"Type '{type.FullName ?? type.Name}' cannot be assigned to {typeof(TValue).ToFormattedTypeName()}.",
                        @throw: @throw
                    );
                    return false;
                }
                return true;
            }

            bool localTryGetConstructorInfo(out ConstructorInfo? ctor)
            {
                ctor = type!.GetConstructor(Type.EmptyTypes);
                if (ctor is null)
                {
                    _ = typeof(TValue).ThrowSoft<MissingMethodException>(
                        messageOrId: $@"Type '{type.FullName ?? type.Name}' lacks a public parameterless constructor.",
                        @throw: @throw
                    );
                    return false;
                }
                return true;
            }

            bool localIsIntrinsicActivatable()
            {
                // Structs (including Nullable<T>), string, Guid, and temporal types
                if (type!.IsValueType
                    || type == typeof(string)
                    || type == typeof(Guid)
                    || type == typeof(DateTime)
                    || type == typeof(DateTimeOffset)
                    || type == typeof(TimeSpan))
                {
                    return true;
                }
                return false;
            }
            #endregion L o c a l   F x
        }

        /// <summary>
        /// Interpets (unsafe) any enum value to its corresponding NotifyCollectionChangedAction.
        /// </summary>
        public static NotifyCollectionChangedAction ToBCLAction(this Enum extendedAction)
        {
            ulong raw = Convert.ToUInt64(extendedAction) & 0x7;
            return (NotifyCollectionChangedAction)raw;
        }

        public static T AsEnumType<T>(this Enum? from, Enum? mask)
            where T : Enum => mask switch
            {
                null => from.AsEnumType<T>(),
                _ => from.AsEnumType<T>(Convert.ToUInt64(mask)),
            };

        /// <summary>
        /// Interpets an Enum instance as the named enum type T without regard for safety or compatibility.
        /// </summary>
        /// <remarks>
        /// This is especially useful for when the receiver is type Enum rather than a named enum value.
        /// </remarks>
        [Canonical]
        public static T AsEnumType<T>(this Enum? from, ulong? mask = null)
            where T : Enum
        {
            if (from is null)
            {
                return Enum.ToObject(typeof(T), -1).SafeAs<T>();
            }
            else
            {
                ulong raw = Convert.ToUInt64(from);
                ulong masked = mask is null ? raw : (raw & mask.Value);
                return Enum.ToObject(typeof(T), masked).SafeAs<T>();
            }
        }

        /// <summary>
        /// Returns true if untyped bitmask flags are found in source.
        /// </summary>
        public static bool HasFlags(this Enum @this, Enum flags)
        {
            var mask = Convert.ToUInt64(flags);
            return (Convert.ToUInt64(@this) & mask) == mask;
        }


        /// <summary>
        /// Adds an empty entry to the builder.
        /// </summary>
        /// <remarks>
        /// Typically builder is later joined with <see cref="Environment.NewLine"/> and this entry appears as a blank line. 
        /// </remarks>
        public static void AddEmpty(this List<string> builder)
            => builder.Add(string.Empty);

        /// <summary>
        /// Fluently adds an empty entry to the builder.
        /// </summary>
        /// <remarks>
        /// Typically builder is later joined with <see cref="Environment.NewLine"/> and this entry appears as a blank line. 
        /// </remarks>
        public static List<string> WithAddEmpty(this List<string> builder)
        {
            builder.Add(string.Empty);
            return builder;
        }

        /// <summary>
        /// Fluent Extension
        /// </summary>
        public static T WithCollectionChangingEvent<T>(
            this T @this,
            NotifyCollectionChangingEventHandler onCollectionChanging
        ) 
        where T : INotifyCollectionChanging
        {
            @this.CollectionChanging += onCollectionChanging;
            return @this;
        }

        /// <summary>
        /// Fluent Extension
        /// </summary>
        public static T WithCollectionChangedEvent<T>(
            this T @this,
            NotifyCollectionChangedEventHandler onCollectionChanged
        ) 
        where T : INotifyCollectionChanged
        {
            @this.CollectionChanged += onCollectionChanged;
            return @this;
        }

        /// <summary>
        /// Fluent Extension
        /// </summary>
        public static T WithCollectionChangeEvents<T>(
            this T @this,
            NotifyCollectionChangingEventHandler? onCollectionChanging,
            NotifyCollectionChangedEventHandler? onCollectionChanged
        ) 
        where T : INotifyCollectionChanging, INotifyCollectionChanged
        {
            if (onCollectionChanging is not null)
            {
                @this.CollectionChanging += onCollectionChanging;
            }
            if (onCollectionChanged is not null)
            {
                @this.CollectionChanged += onCollectionChanged;
            }
            return @this;
        }

        public static string Indent(this string @this, int spaces)
        => @this.Pad(string.Join(string.Empty, Enumerable.Repeat(' ', spaces)));

        public static string Pad(this string @this, string pad)
        => $"{pad}{@this.Replace("\n", $"\n{pad}")}";

        /// <summary>
        /// Evaluates the state of a list-like object.
        /// </summary>
        /// <remarks>
        /// Detects both generic and non-generic lists. Falls back to reflection when
        /// no direct IList interface is present. Returns <see cref="StatusAsList.NotSupported"/>
        /// for types without a countable interface.
        /// </remarks>
        public static StatusAsList GetStatusAsList(this object? items)
        {
            if (items is EventArgs)
            {
#if DEBUG
                // Seriously hard throw in dev.
                throw new NotSupportedException(
                    $"Illegal invocation on {items.GetType().Name}. Call e.g. on NewItems or OldItems instead.");
#else
                return StatusAsList.NotSupported;
#endif
            }
            // Null receiver.
            if (items is null)
            {
                return StatusAsList.Null;
            }

            // Non-generic list.
            if (items is IList listA)
            {
                return listA.Count switch
                {
                    0 => StatusAsList.Empty,
                    1 => StatusAsList.Single,
                    _ => StatusAsList.Multi,
                };
            }
            // Reflection fallback for any type exposing a Count property.
            if (items.GetType().GetProperty(nameof(IList.Count)) is { } pi)
            {
                return pi.GetValue(items) switch
                {
                    0 => StatusAsList.Empty,
                    1 => StatusAsList.Single,
                    _ => StatusAsList.Multi,
                };
            }

            // No count semantics detected.
            return StatusAsList.NotSupported;
        }

        /// <summary>
        /// Iterates key objects for ancestors (and optionally self).
        /// </summary>
        public static IEnumerable Ancestors(this IDictionary @this, bool includeSelf = false)
        {
            if (BriskDictionary.ReverseLookup[@this] is { } bdw)
            {
                var ancs =
                    includeSelf
                    ? bdw.XDUNK.AncestorsAndSelf()
                    : bdw.XDUNK.Ancestors();

                foreach (var anc in ancs)
                {
                    if (anc.Attribute(nameof(StdCollectionXAttribute.key)) is XBoundAttribute xba)
                    {
                        yield return xba.Tag;
                    }
                }
            }
        }

        public static bool TryGetWhereAttribute(this Enum @this, out string binding, out string expr, bool @throw = false)
        {
            if (@this.GetCustomAttribute<WhereAttribute>() is { } attr)
            {
                binding = attr.Binding;
                expr = attr.Expr;
                return true;
            }
            else
            {
                if (@throw)
                {
                    @this.ThrowSoft<InvalidOperationException>(
                        $"Advisory failed {nameof(TryGetWhereAttribute)}", @throw: @throw);
                }
                binding = expr = string.Empty;
                return false;
            }
        }

        public static bool TryGetTrackAttribute(this Enum @this, out string binding, out string expr, bool @throw = false)
        {
            if (@this.GetCustomAttribute<WhereAttribute>() is { } attr)
            {
                binding = attr.Binding;
                expr = attr.Expr;
                return true;
            }
            else
            {
                if (@throw)
                {
                    @this.ThrowSoft<InvalidOperationException>(
                        $"Advisory failed {nameof(TryGetWhereAttribute)}", @throw: @throw);
                }
                binding = expr = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Walks the inheritance chain to return most derived declared instance property.
        /// </summary>
        public static PropertyInfo? GetMostDerivedProperty(
            this Type type,
            string propertyName,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        {
            for (var t = type; t is not null; t = t.BaseType)
            {
                if (t.GetProperty(propertyName, flags) is { } pi)
                {
                    return pi;
                }
            }
            return null;
        }
    }
}