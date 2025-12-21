using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    public static class BriskExtensions
    {
        [Canonical]
        internal static BriskKeyTriage TriageSubkey(this object unk)
        {
            if (unk is null)
                return BriskKeyTriage.Null;

            // Recognize Type objects first.
            if (unk is Type)
                return BriskKeyTriage.Type;

            // Strings are handled explicitly, not as reference types.
            if (unk is string)
                return BriskKeyTriage.String;

            var type = unk.GetType();

            // Value types (structs, enums, primitives) are immutable literals.
            if (type.IsPrimitive)
                return BriskKeyTriage.Value;

            // Everything else is a reference type instance.
            return BriskKeyTriage.Reference;
        }

        /// <summary>
        /// Formats the type signature of a dictionary as [TKey:TValue].
        /// Falls back to [object:object] if the IDictionary is non-generic.
        /// </summary>
        [Canonical("For this NuGet and others")]
        public static string ToFormattedDictName(
            this IDictionary? dict,
            params Enum[] options)
        {
            if (dict is null)
            {
                return "[null]";
            }

            // Defaults
            FormattedTypeNameOptionFlag typeOptions = 0;
            bool isTypeOptionDefault = true;
            FormattedDictNameOptionFlag dictOptions = 0;
            bool isDictOptionDefault = true;
            foreach (var optionUnk in options)
            {
                switch (optionUnk)
                {
                    case FormattedTypeNameOptionFlag option:
                        isTypeOptionDefault = false;
                        if (option == 0)
                        {
                            typeOptions = 0;
                        }
                        else
                        {
                            typeOptions |= option;
                        }
                        break;
                    case FormattedDictNameOptionFlag option:
                        isDictOptionDefault = false;
                        if (option == 0)
                        {
                            dictOptions = 0;
                        }
                        else
                        {
                            dictOptions |= option;
                        }
                        break;
                }
            }
            if (isTypeOptionDefault)
            {
                typeOptions = FormattedTypeNameOptionFlag.UseShortTypeName;
            }
            if (isDictOptionDefault)
            {
                dictOptions = FormattedDictNameOptionFlag.Personality;
            }

            var builder = new List<string>();

            var type = dict.GetType();
            var iface = type
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictOptions!.HasFlag(FormattedDictNameOptionFlag.Personality))
            {
                var personality = dict switch
                {
                    ITolerant => "tolerant",
                    IInsistent => "insistent",
                    _ => "dict"
                };
                builder.Add($"({personality})");
            }

            if (iface is null)
            {
                builder.Add("[object:object]");
            }
            else
            {
                var args = iface.GetGenericArguments();
                builder.Add(
                    $"[{args[0].ToFormattedTypeName(typeOptions)}:{args[1].ToFormattedTypeName(typeOptions)}]");
            }

            if (dictOptions.HasFlag(FormattedDictNameOptionFlag.Count))
            {
                builder.Add($" Count={dict.Count}");
            }

            var preview = string.Join(string.Empty, builder);
            return preview;
        }

        /// <summary>
        /// Formats the type signature of an unkown key.
        /// </summary>
        [Canonical]
        public static string ToFormattedKeyName(this object key, int max = 20, bool bracketed = false)
        {
            BriskKeyTriage bkt;
            switch ((bkt = key.TriageSubkey()))
            {
                case BriskKeyTriage.Null: return localFormat("null", bracketed: bracketed);
                case BriskKeyTriage.Type: return localFormat($"{((Type)key).ToFormattedTypeName(FormattedTypeNameOptionFlag.UseShortTypeName)}", bracketed: bracketed);
                case BriskKeyTriage.String: return localFormat(key, max, bracketed: bracketed);
                case BriskKeyTriage.Value: return localFormat(key, max, bracketed: bracketed);
                case BriskKeyTriage.Reference: return localFormat($"ref({key.GetType().Name})", bracketed: bracketed);
                default:
                    throw new NotImplementedException($"Bad case: {bkt}");
            }

            /// <summary>
            /// Removes whitespace and control characters, truncates if needed,
            /// and appends ellipsis when the string exceeds the specified length.
            /// </summary>
            /// <remarks>
            /// The heuristic distinguishes between different key "personalities" so that
            /// each is represented meaningfully in string form without leaking excessive detail.
            /// - <c>Null</c>: Returned literally as "null".
            /// - <c>Type</c>: Reports only the simple name of the type.
            /// - <c>String</c>: Shown verbatim up to a 15-character limit with non-printable
            ///   characters removed.
            /// - <c>Value</c>: Shown up to 10 characters; used for primitive or value types.
            /// - <c>Reference</c>: Emits the runtime type name prefixed with "ref:".
            /// This balance of brevity and precision provides stable diagnostic output across
            /// anonymous and reflected key types while avoiding noisy or potentially misleading
            /// object representations.
            /// </remarks>
            static string localFormat(
                object? raw, 
                int? max = null,
                bool bracketed = false,
                bool useParenForGenericType = true)
            {
                string lint;
                if (raw is null)
                {
                    lint = "null";
                }
                else
                {
                    // Convert to string representation.
                    string text = raw.ToString() ?? string.Empty;

                    // Filter out whitespace and non-printable characters.
                    lint = new string(
                        text.Where(c =>
                            !char.IsWhiteSpace(c) &&
                            !char.IsControl(c)).ToArray());

                    // Apply length limit if provided.
                    if (max is int limit && lint.Length > limit)
                    {
                        lint = lint[..limit] + "...";
                    }
                    // Remove tag notation
                    if(useParenForGenericType) lint = lint.Replace('<', '(').Replace('>', ')');
                }
                return
                    bracketed
                    ? $"[{lint}]"
                    : lint;
            }
        }

        /// <summary>
        /// Flattens any nested IEnumerable objects within the provided key sequence,
        /// excluding strings, into a single array of objects.
        /// </summary>
        public static object[] UnrollKeyChainObjects(this object key, params object[] moreKeys)
        {
            var result = new List<object>();
            void add(object? item)
            {
                if (item is null)
                {
                    result.Add(null!);
                    return;
                }

                // Treat string as atomic, not as IEnumerable<char>.
                if (item is string)
                {
                    result.Add(item);
                    return;
                }

                // Flatten IEnumerable items recursively.
                if (item is IEnumerable enumerable)
                {
                    foreach (var sub in enumerable)
                    {
                        add(sub!);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            add(key);
            foreach (var k in moreKeys)
            {
                add(k);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Builds a deterministic, path-safe string representation from a composite key chain.
        /// </summary>
        /// <remarks>
        /// Converts supported key types such as enums, types, strings, GUIDs, and temporal values 
        /// into canonical text components joined by path separators. The result is locale-invariant 
        /// and suitable for use in cache or registry paths.
        /// </remarks>
        [Canonical("The gold standard for creating brisk key chains.")]
        public static string MakePathFromObjects(this object[] keyChain)
        {
            object key;
            var builder = new List<string>();
            for (int i = 0; i < keyChain.Length; i++)
            {
                key = keyChain[i];
                switch (key)
                {
                    case Enum @enum:
                        var enumType = @enum.GetType();

                        // Pull the path from the enum itself.
                        if (enumType.GetCustomAttribute<KeySegmentAttribute>() is { } attr)
                        {
                            switch (attr)
                            {
                                case AbsoluteKeySegmentAttribute:
                                    builder.Add(@enumType.Name);
                                    break;
                                case RelativeKeySegmentAttribute:
                                    // N O O P
                                    break;
                                default:
                                    Debug.Fail($@"ADVISORY - First Time.");
                                    break;
                            }
                            if (attr.KeyChainObjects.Any())
                            {
                                builder.AddRange(attr.KeyChainObjects);
                            } 
                            // DIFFERENT! Just the member name here
                            builder.Add(@enum.ToString());

                            // Look for a relative path attached to this specific member.
                            if(@enum.GetCustomAttribute<RelativeKeySegmentAttribute>() is { } rel)
                            {
                                builder.AddRange(rel.KeyChainObjects);
                            }
                        }
                        else 
                        {
                            // DIFFERENT! Enums that are not indexers key at {Type}.{Member}.
                            builder.Add(@enum.ToFullKey());
                        }
                        break;

                    case Type type:
                        builder.Add(type.ToFormattedKeyName());
                        break;

                    case string @string:
                        builder.Add(@string);
                        break;

                    case Guid guid:
                        builder.Add(guid.ToString("N").ToUpper());
                        break;

                    case Uri uri:
                        // Preserve absolute form without encoding, safe for path component.
                        builder.Add(uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString);
                        break;

                    // --- Temporal handling begins here ---
                    case DateTime dt:
                        // Use sortable format with UTC normalization for stability.
                        builder.Add(dt.ToUniversalTime().ToString("yyyyMMddTHHmmssZ"));
                        break;

                    case DateOnly date:
                        builder.Add(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                        break;

                    case TimeOnly time:
                        builder.Add(time.ToString("HHmmss", CultureInfo.InvariantCulture));
                        break;

                    case TimeSpan span:
                        // ISO-8601 duration-style component, trimmed for readability.
                        builder.Add($"T{span:hh\\-mm\\-ss}");
                        break;
                        // --- Temporal handling ends here ---
                    default:
                        if (key.GetType().IsValueType)
                        {
                            // For structs and numerics, use a stable ToString with invariant culture
                            builder.Add(Convert.ToString(key, CultureInfo.InvariantCulture)!);
                            continue;
                        }
                        builder.Add($"{key.GetType().Name}:{RuntimeHelpers.GetHashCode(key)}");
                        break;
                }
            }
            return Path.Combine(builder.ToArray());
        }


        /// <summary>
        /// Splits comma-separated arguments into a flat sequence of strings.
        /// </summary>
        /// <remarks>
        /// Each argument may itself be a CSV string. Commas and surrounding whitespace
        /// are trimmed, and empty segments are ignored. This permits flexible call sites
        /// where the caller may pass either a single combined CSV or multiple discrete args.
        /// </remarks>
        public static string[] UnrollCsvArgs(this string arg, params string[] moreArgs)
        {
            IEnumerable<string> split(string csv) =>
                csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return split(arg)
                .Concat(moreArgs.SelectMany(split))
                .ToArray();
        }

        /// <summary>
        /// Ensures unique string keys by appending "#00", "#01", and so on
        /// to all occurrences when duplicates are found.
        /// </summary>
        /// <remarks>
        /// - Keys that occur once pass through unchanged.
        /// - String keys with duplicates are all suffixed starting at "#00".
        /// - Non-string keys are ignored and emitted unchanged.
        /// - The original enumeration order is preserved.
        /// </remarks>
        public static IEnumerable<DictionaryEntryPreview> WithDuplicateNamesIndexed(
            this IEnumerable<DictionaryEntryPreview> source)
        {
            var array = source.ToArray();

            // Capture original index for restoration.
            var indexed = array
                .Select((entry, index) => new { entry, index })
                .ToList();

            // Group only string keys.
            var stringGroups = indexed
                .Where(x => x.entry.Key is string)
                .GroupBy(x => (string)x.entry.Key!)
                .ToDictionary(g => g.Key, g => g.ToArray());

            // Build results by index.
            var results = new Dictionary<int, DictionaryEntryPreview>();

            foreach (var item in indexed)
            {
                if (item.entry.Key is string s && stringGroups.TryGetValue(s, out var group))
                {
                    if (group.Length == 1)
                    {
                        // Unique key: unchanged.
                        results[item.index] = item.entry;
                    }
                    else
                    {
                        // Shared key: suffix all.
                        var i = Array.IndexOf(group, item);
                        var newKey = $"{s}#{i:D2}";
                        results[item.index] = new DictionaryEntryPreview(newKey, item.entry.Value);
                    }
                }
                else
                {
                    // Non-string key: untouched.
                    results[item.index] = item.entry;
                }
            }

            // Re-emit in original order.
            return results
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value);
        }

        /// <summary>
        /// Returns only non-special methods declared on the specified type.
        /// </summary>
        /// <remarks>
        /// Filters out compiler-generated accessors such as get/set and add/remove
        /// while optionally restricting results to methods declared directly on
        /// the specified type. The result is suitable for reflection caches
        /// where only user-defined method declarations are of interest.
        /// </remarks>
        public static IEnumerable<MethodInfo> GetDeclaredUserMethods(this Type type, bool includeInherited = false)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public;
            if (!includeInherited)
            {
                flags |= BindingFlags.DeclaredOnly;
            }

            return type
                .GetMethods(flags)
                .Where(mi => !mi.IsSpecialName);
        }

        public static bool TryGetHost<TKey, TValue>(this IDictionary<TKey, TValue> dunk, out BriskDictionaryWrapper bdw)
            => (dunk as IDictionary).TryGetHost(out bdw);

        [Canonical]
        public static bool TryGetHost(this IDictionary? dunk, out BriskDictionaryWrapper bdw)
        {
            if (dunk is null)
            {
                bdw = null!;
                return false;
            }
            else
            {
                bdw = BriskDictionary.ReverseLookup[dunk]!;
                return bdw is not null;
            }
        }

        public static XElement? ToXDunk<TKey, TValue>(
            this IDictionary<TKey, TValue> @this,
            bool @throw = false)
            where TKey : notnull
            => (@this as IDictionary).ToXDunk(@throw);

        /// <summary>
        /// If the IDictionary is hosted, returns the XElement that hosts it.
        /// </summary>
        /// <remarks>
        /// Once retrieved, feel free to use XBoundAttributes (just don't destroy the IDictionary);
        /// </remarks>        
        [Canonical]
        public static XElement? ToXDunk(this IDictionary? @this, bool @throw = false)
        {
            if (@this.TryGetHost(out BriskDictionaryWrapper bdw))
            {
                return bdw.XDUNK;
            }
            else
            {
                if (@throw)
                {
                    @this.ThrowHard<InvalidOperationException>(
                        $"Receiver must be registered in {nameof(BriskDictionary.ReverseLookup)}.");
                }
                return null;
            }
        }


        /// <summary>
        /// Returns or upgrade the current dictionary as, or upgrade to, the new strong type pair.
        /// </summary>
        /// <remarks>
        /// If this operation fails, it will return null without warning unless @throw is set true.
        /// </remarks>
        public static IObservableDictionary<TKey, TValue> AsStronglyTypedDictionary<TKey, TValue>(
            this IObservableDictionary dunk,
            bool @throw = false)
            where TKey : notnull
            => dunk.AsStronglyTypedDictionary<TKey, TValue>(mode: null, out _, @throw);

        /// <summary>
        /// Attempts to return or upgrade the current dictionary as, or upgrade to, the new strong type pair.
        /// </summary>
        public static IObservableDictionary<TKey, TValue> AsStronglyTypedDictionary<TKey, TValue>(
            this IObservableDictionary dunk,
            out StrongTypesUpgradeStatus result,
            bool @throw = false) 
            where TKey : notnull
            => dunk.AsStronglyTypedDictionary<TKey, TValue>(mode: null, out result, @throw);

        /// <summary>
        /// Returns a strongly typed InsistentDictionary with the specified activation dlgt
        /// </summary>
        /// <remarks>
        /// If this operation fails, it will return null without warning unless @throw is set true.
        /// </remarks>
        public static IObservableDictionary<TKey, TValue> AsStronglyTypedDictionary<TKey, TValue>(
            this IObservableDictionary dunk,
            Func<TValue>? activationDlgt,
            bool @throw = false)
            where TKey : notnull
            where TValue : notnull
        {
            var preview = (IInsistentDictionary <TKey, TValue>)dunk.AsStronglyTypedDictionary<TKey, TValue>(
                DictionaryMode.InsistentNotNull, 
                out _, 
                @throw);
            preview.ActivationDlgt = activationDlgt;
            return preview;
        }

        /// <summary>
        /// Returns or upgrade the current dictionary as, or upgrade to, the new strong type pair.
        /// </summary>
        /// <remarks>
        /// If this operation fails, it will return null without warning unless @throw is set true.
        /// </remarks>
        public static IObservableDictionary<TKey, TValue> AsStronglyTypedDictionary<TKey, TValue>(
            this IObservableDictionary dunk,
            DictionaryMode? mode,
            bool @throw = false)
            where TKey : notnull
            => dunk.AsStronglyTypedDictionary<TKey, TValue>(mode,  out _, @throw);

        /// <summary>
        /// Attempts to return or upgrade the current dictionary as, or upgrade to, the new strong type pair.
        /// </summary>
        [Canonical]
        private static IObservableDictionary<TKey, TValue> AsStronglyTypedDictionary<TKey, TValue>(
            this IObservableDictionary dunk,
            DictionaryMode? mode,
            out StrongTypesUpgradeStatus result,
            bool @throw = false) where TKey : notnull
        {
            if(dunk.TryGetHost(out BriskDictionaryWrapper bdw))
            {
                // Discard the result, as we are relying on
                // the error handling in the class itself.
                _ = bdw.TryStrongTypesUpgrade(mode, out IObservableDictionary<TKey, TValue>? stronglyTyped, out result, @throw);
                return stronglyTyped!;
            }
            else
            {
                result = StrongTypesUpgradeStatus.NotUpgradable;
                if(@throw)
                {
                    dunk.ThrowHard<NotSupportedException>($"The {nameof(IDictionary)} handle could not be cast to {nameof(BriskDictionaryWrapper)}");
                }
                return null!;
            }
        }
    }
}
