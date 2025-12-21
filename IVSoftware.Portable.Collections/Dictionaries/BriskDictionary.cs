using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.Portable.Xml.Linq.XBoundObject.Placement;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    public class BriskDictionary
        : InsistentDictionary<IDictionary, BriskDictionaryWrapper>
        , IBriskDictionary
    {
        public IObservableDictionary this[object key, params object[] moreKeys]
        {
            get
            {
                BriskDictionaryWrapper bdw;

                object[] keyChain = key.UnrollKeyChainObjects(moreKeys);

                XElement xdunk = null!; // Placer will place.
                object[] progressive = [];
                string path;
                PlacerResult result;

                foreach (var unk in keyChain)
                {
                    progressive = progressive.Concat([unk]).ToArray();
                    path = progressive.MakePathFromObjects();
                    result = Model.Place(path, out xdunk);
                    switch (result)
                    {
                        case PlacerResult.Exists:
                            break;
                        case PlacerResult.Created:
                            xdunk.SetBoundAttributeValue(
                                tag: unk,
                                name: nameof(StdCollectionXAttribute.key),
                                text: "[KeyObject]");
                            break;
                        default:
                            this.ThrowHard<NotSupportedException>($"The {result.ToFullKey()} case is not expected.");
                            return null!; 
                    }
                }

                bdw = xdunk.To<BriskDictionaryWrapper>();
                if (bdw is null)
                {
                    bdw = new BriskDictionaryWrapper(xdunk, DefaultActivationType);
                    if(bdw.XDUNK.Parent is not null)
                    {
                        OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(action: NotifyCollectionChangedAction.Add, changedItem: bdw));
                    }
                }
                if( keyChain.Last() is Enum @enum &&
                    @enum.GetType().GetCustomAttribute<KeySegmentAttribute>() is not null &&
                    @enum.GetCustomAttribute<StrongTypedDictionaryAttribute>() is { } stda)
                {
                    Type
                        tKey = stda.TKey,
                        tValue = stda.TValue;

                    _method ??=
                        typeof(BriskDictionaryWrapper)
                            .GetMethod(
                                nameof(BriskDictionaryWrapper.TryStrongTypesUpgrade),
                                BindingFlags.Instance | BindingFlags.NonPublic)!
                            .MakeGenericMethod(tKey, tValue);

                    var parameters = new object?[]
                    {
                        bdw.@base.Mode,   // DictionaryMode? mode
                        null,       // out IObservableDictionary<TKey, TValue>? stronglyTyped
                        null,       // out StrongTypesUpgradeStatus status
                        false       // bool @throw
                    };

                    if(!(bool)_method.Invoke(bdw, parameters)!)
                    {
                        bdw.ThrowHard<InvalidOperationException>($"Failed inline strong type upgrade from {@enum.ToFullKey()}.");
                    }
                }
                return bdw.@base;
            }
        }
        private MethodInfo? _method = null;
        public XElement Model { get; } = new XElement(nameof(StdCollectionXElement.model));

        public event EventHandler<ExpandXKeyFormatRequestedEventArgs>? ExpandXKeyFormatRequested;
        public static event EventHandler<ExpandXKeyFormatRequestedEventArgs>? AnyExpandXKeyFormatRequested;

        public static ReverseLookup ReverseLookup = new();

        public override void Clear()
        {
            Model.RemoveAll();
            base.Clear();
        }

        public bool ContainsKey(object key, params object[] moreKeys)
        {
            Debug.Fail($@"ADVISORY - First Time.");
            object[] unrolled = key.UnrollKeyChainObjects(moreKeys);
            var path = unrolled.MakePathFromObjects();
            return PlacerResult.Exists == Model.Place(path, PlacerMode.FindOrPartial);
        }

        /// <summary>
        /// Produces an XML string representing the current model with all nested dictionaries expanded.
        /// </summary>
        /// <remarks>
        /// This method temporarily augments the model with a <values> element for each dictionary,
        /// including its key–value pairs, then restores the model to its prior state when complete.
        /// The <see cref="ExpandXKeyFormatRequested"/> event allows customization of each <kvp> node before it
        /// is added to the output.
        /// </remarks>
        public string ViewExpandedModel()
        {
            string expandedModelView = string.Empty;
            using var _ = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    foreach (var xdunk
                             in Model.Descendants()
                                .Where(_ => _.Has<BriskDictionaryWrapper>())
                                .ToArray())
                    {
                        var bdw = xdunk.To<BriskDictionaryWrapper>();
                        var @base = bdw.@base;

                        // Make a Values node.
                        var xvalues = new XElement(
                            nameof(StdCollectionXElement.values),
                            new XAttribute(nameof(StdCollectionXAttribute.count), @base.Count));
                        foreach (var key in bdw.@base.Keys)
                        {
                            var value = @base[key];
                            var desc = localToStringHeuristic(value);

                            var xentry = new XElement(
                                nameof(StdCollectionXElement.entry), 
                                new XAttribute(nameof(StdCollectionXAttribute.type), value?.GetType().Name ?? "null"));
                            var xkey = new XElement(nameof(StdCollectionXElement.key), key.ToFormattedKeyName());
                            var xvalue = new XElement(nameof(StdCollectionXElement.value), desc);

                            xentry.Add(xkey);
                            xentry.Add(xvalue);
                            AnyExpandXKeyFormatRequested?.Invoke(this, new ExpandXKeyFormatRequestedEventArgs(xkey, valueToFormat: value));
                            ExpandXKeyFormatRequested?.Invoke(this, new ExpandXKeyFormatRequestedEventArgs(xkey, valueToFormat: value));
                            xvalues.Add(xentry);
                        }
                        xdunk.Add(xvalues);
                    }
                    expandedModelView = Model.ToString();

                    #region L o c a l F x 
                    static string localToStringHeuristic(object? o)
                    {
                        switch (o)
                        {
                            case null: 
                                return "null";
                            case string @string:
                                return @string;
                            case IDictionary dict:
                                return dict.ToFormattedDictName();
                            case Enum @enum:
                                return @enum.ToFullKey();
                            case IEnumerable unks:
                                return string.Join(
                                    Environment.NewLine,
                                    unks.OfType<object>().Select(_ => localToStringHeuristic(_)));
                            case ConstructorInfo:
                            case MethodInfo:
                            case EventInfo:
                                return o?.ToString() ?? o.GetType().Name;
                            case PropertyInfo pi:
                                return pi.PropertyType.Name;
                            case Type type:
                                return type.ToFormattedTypeName();
                            default:
                                return o.ToString() ?? "null";
                        }
                    }
                    #endregion L o c a l F x
                },
                onDispose: (sender, e) =>
                {
                    foreach (var tmp
                             in Model
                                .Descendants("values")
                                .ToArray())
                    {
                        tmp.Remove();
                    }
                });
            return expandedModelView;
        }

        /// <summary>
        /// The type that is activated to satisfy insistent 
        /// calls for non-existent keys.
        /// </summary>
        public Type DefaultActivationType
        {
            get => _defaultActivationType;
            set
            {
                if (!Equals(_defaultActivationType, value))
                {
                    if (value.GetConstructor(Type.EmptyTypes) is null)
                    {
                        this.ThrowHard<InvalidOperationException>(
$@"Type '{value.FullName ?? value.Name}' is missing a public parameterless constructor.
{nameof(DefaultActivationType)} remains set to {_defaultActivationType.ToFormattedTypeName()}."
                        );
                    }
                    else
                    {
                        _defaultActivationType = value;
                    }
                }
            }
        }
        Type _defaultActivationType = typeof(TolerantDictionary<object, object>);
    }
}
