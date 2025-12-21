using IVSoftware.Portable.Collections.MSTest.Mutator;
using IVSoftware.Portable.Collections.MSTest.TestTargets;
using System.Collections;
using System.Text.RegularExpressions;

namespace IVSoftware.Portable.Collections.MSTest.TestUtils
{
    internal static class MSTestExtensions
    {
        /// <summary>
        /// 1. Removes assembly and version information from fully qualified type names,
        ///    trimming details after the first comma in each <c>AssignedType</c> or <c>AssignedKeyType</c> string.
        /// 2. Expands any numeric <c>Modified</c> field into its <see cref="ModifiedFlag"/> enumeration name
        /// </summary>
        [Obsolete("Use the 'tojson' code snippet to make a custom serializer ad hoc.")]
        public static string WithSimpleFormatting(this string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            // Pass 1: simplify fully qualified type names.
            const string typePattern = "(\"Assigned(?:Key)?Type\"\\s*:\\s*\")([^,\"]+)(?:,[^\"]*)?\"";
            const string typeReplacement = "$1$2\"";
            json = Regex.Replace(json, typePattern, typeReplacement);

            // Pass 2: expand Modified flag integers into their enumeration names.
            const string modifiedPattern = "(\"Modified\"\\s*:\\s*)(\\d+)";
            json = Regex.Replace(json, modifiedPattern, match =>
            {
                var prefix = match.Groups[1].Value;
                var numberText = match.Groups[2].Value;

                if (int.TryParse(numberText, out var value))
                {
                    var flags = (ModifiedFlag)value;
                    var rendered = flags == ModifiedFlag.NoChanges
                        ? nameof(ModifiedFlag.NoChanges)
                        : flags.ToString();
                    return $"{prefix}\"{rendered}\"";
                }

                return match.Value; // fallback, preserve original
            });

            return json;
        }

        static int _autoCoerceCount = 0;
        public static void ClearAutoCoerceCount(this object _) => _autoCoerceCount = 0;

        public static NotifyCollectionChangingAction ToNotifyCollectionChangingAction(this OCMCall @this)
            => @this switch
            {
                OCMCall.Add => NotifyCollectionChangingAction.Add,
                OCMCall.AddRange => NotifyCollectionChangingAction.Add,
                OCMCall.AddDistinct => NotifyCollectionChangingAction.Add,
                OCMCall.AddRangeDistinct => NotifyCollectionChangingAction.Add,
                OCMCall.Clear => NotifyCollectionChangingAction.Reset,
                OCMCall.Replace => NotifyCollectionChangingAction.Replace,
                OCMCall.Insert => NotifyCollectionChangingAction.Add,
                OCMCall.InsertRange => NotifyCollectionChangingAction.Add,
                OCMCall.Move => NotifyCollectionChangingAction.Move,
                OCMCall.Remove => NotifyCollectionChangingAction.Remove,
                OCMCall.RemoveAt => NotifyCollectionChangingAction.Remove,
                OCMCall.RemoveRange => NotifyCollectionChangingAction.Remove,
                OCMCall.RemoveMultiple => NotifyCollectionChangingAction.Remove,
                _ => throw new NotImplementedException(),
            };


        public static string ParseReplace(
            this string json,
            DictionaryEntry primary,
            params DictionaryEntry[] additional)
        {
            if (json is null)
            {
                return string.Empty;
            }

            json = localApply(json, primary);

            foreach (var kvp in additional)
            {
                json = localApply(json, kvp);
            }

            return json;

            static string localApply(string json, DictionaryEntry kvp)
            {
                if (kvp.Key is not string key)
                {
                    return json;
                }

                if (kvp.Value is not Type enumType || !enumType.IsEnum)
                {
                    return json;
                }

                // Matches: "Selection": 1
                var pattern = $@"""(?<key>{Regex.Escape(key)})""\s*:\s*(?<value>-?\d+)";
                return Regex.Replace(
                    json,
                    pattern,
                    match =>
                    {
                        var raw = match.Groups["value"].Value;
                        if (!long.TryParse(raw, out var numeric))
                        {
                            return match.Value;
                        }

                        var enumValue = Enum.ToObject(enumType, numeric);

                        var name =
                            enumType.IsDefined(typeof(FlagsAttribute), inherit: false)
                                ? enumValue.ToString()
                                : Enum.GetName(enumType, enumValue) ?? raw;

                        return $@"""{key}"": ""{name}""";
                    });
            }
        }


        public static IList PopulateDemoItems(this object? @this)
        {
            var items = new List<ItemCardModel>();
            int id = 0;
            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Apple",
                Keywords = @"[""fruit"", ""red"", ""sweet""]",
                Tags = "fruit produce",
                IsChecked = true
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Banana",
                Keywords = @"[""fruit"", ""yellow"", ""soft""]",
                Tags = "fruit produce",
                IsChecked = false
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Carrot",
                Keywords = @"[""vegetable"", ""orange"", ""root""]",
                Tags = "vegetable produce",
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Broccoli",
                Keywords = @"[""vegetable"", ""green"", ""cruciferous""]",
                Tags = "vegetable produce",
                IsChecked = true
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Strawberry",
                Keywords = @"[""fruit"", ""red"", ""berry""]",
                Tags = "fruit produce berry",
                IsChecked = false
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Spinach",
                Keywords = @"[""vegetable"", ""leafy"", ""green""]",
                Tags = "vegetable produce leafy",
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Orange",
                Keywords = @"[""fruit"", ""citrus"", ""orange""]",
                Tags = "fruit produce citrus",
                IsChecked = true
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Tomato",
                Keywords = @"[""fruit"", ""red"", ""savory""]",
                Tags = "fruit vegetable produce",
                IsChecked = false
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Cucumber",
                Keywords = @"[""vegetable"", ""green"", ""fresh""]",
                Tags = "vegetable produce",
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Blueberry",
                Keywords = @"[""fruit"", ""blue"", ""small""]",
                Tags = "fruit produce berry",
                IsChecked = true
            });

            if (@this is IRangeable rangeable)
            {
                rangeable.AddRange(items);
            }
            return items;
        }

#if false

        /// <summary>
        /// Returns a JSON representation suitable for test comparison.
        /// </summary>
        /// <remarks>
        /// Supports the extended <see cref="JsonFormatting"/> flags for outer and nested indentation.
        /// </remarks>
        public static string ToJson(this ICoercibleValue @this, params Enum[] options)
        {
            string preview;
            // Default ONLY if no other options present OF ANY KIND.
            JsonFormatting? formatting = options.Length == 0 ? JsonFormatting.IndentOuter : null;
            JsonFormattingEx? formattingEx = null;

            foreach (var optionUnk in options)
            {
                switch (optionUnk)
                {
                    case JsonFormatting known:
                        formatting ??= 0;    // Make non null
                        formatting |= known; // OR in one or more options
                        break;
                    case JsonFormattingEx known:
                        if (known == 0)
                        {
                            // Explicit, sequential clear (for when a default value is not null).
                            formattingEx = known;
                        }
                        else
                        {
                            formattingEx ??= 0;    // Make non null
                            formattingEx |= known; // OR in one or more options
                        }
                        break;
                }
            }
            bool indentOuter =
                formatting?.HasFlag(JsonFormatting.IndentOuter) == true
                || formattingEx?.HasFlag(JsonFormattingEx.IndentOuter) == true;
            bool indentInner =
                formatting?.HasFlag(JsonFormatting.IndentInner) == true
                || formattingEx?.HasFlag(JsonFormattingEx.IndentInner) == true;

            string listSeparator = indentOuter ? ",\n  " : ", ";
            string prefix = indentOuter ? "{\n  " : "{ ";
            string suffix = indentOuter ? "\n}" : " }";

            var builderInner = new List<string>();
            if(@this is ICoercibleDictionaryEntry entry)
            {
                builderInner.Add(toJson(nameof(entry.Key), entry.Key));
                builderInner.Add(toJson(nameof(entry.IsKeyCoerced), entry.IsKeyCoerced));
                builderInner.Add(toJson(nameof(entry.AssignedKeyType), entry.AssignedKeyType));
            }
            builderInner.Add(toJson(nameof(@this.Value), @this.Value));
            builderInner.Add(toJson(nameof(@this.IsCoerced), @this.IsCoerced));
            builderInner.Add(toJson(nameof(@this.AssignedType), @this.AssignedType));
            preview = prefix + string.Join(listSeparator, builderInner) + suffix;
            return preview;

            #region L o c a l   F x
            string toJson(string name, object? value)
                => $@"""{name}"": {@as(value)}";

            string @as(object? value)
            {
                if (value is null) return "null";

                if (value is ICoercibleValue coercible)
                {
                    // Inner coercibles inherit indentation if explicitly requested
                    var nestedFormat = indentInner ? JsonFormatting.IndentOuter : JsonFormatting.None;
                    return coercible.ToJson(nestedFormat);
                }

                if (value is string or bool or Type or Enum)
                {
                    return value switch
                    {
                        string s => JsonSerializer.Serialize(s),
                        bool b => JsonSerializer.Serialize(b),
                        Type t => JsonSerializer.Serialize(t.FullName),
                        Enum e => JsonSerializer.Serialize(e.ToFullKey()),
                        _ => JsonSerializer.Serialize(value)
                    };
                }

                try
                {
                    var preview = JsonSerializer.Serialize(value);
                    return preview;
                }
                catch
                {
                    return value?.ToString() ?? string.Empty;
                }
            }
            #endregion L o c a l   F x
        }
#endif
    }
}
