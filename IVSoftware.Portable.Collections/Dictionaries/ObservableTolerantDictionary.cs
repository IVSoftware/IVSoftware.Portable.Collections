using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    /// <summary>
    /// Dictionary variant that tolerates missing keys and null assignments
    /// </summary>
    public class TolerantDictionary<TKey, TValue>
        : ObservableDictionary<TKey, TValue?>
        , ITolerantDictionary<TKey, TValue?>
        where TKey : notnull
    {
        public TolerantDictionary() : this(mode: DictionaryMode.TolerantReturnDefault) { }
        public TolerantDictionary(DictionaryMode mode)
        {
            switch (mode)
            {
                case DictionaryMode.TolerantReturnDefault:
                case DictionaryMode.TolerantCreateDefaultEntry:
                    Mode = mode;
                    break;
                default:
                    this.ThrowHard<ArgumentException>(
                        $"A dictionary of type {GetType().ToFormattedTypeName()} cannot be initialized as {mode.ToFullKey()}");
                    break;
            }
        }
        public override TValue? this[TKey key] 
        { 
            get => this[key, false];
            set => this[key, false] = value;
        }

        /// <summary>
        /// Tolerant Get Pattern
        /// </summary>
        public TValue? this[TKey key, bool @throw = false]
        {
            get
            {
                if (@throw)
                {
                    // Temporary, forced, raw IDictionary throw.
                    return base[key];
                }
                else
                {
                    if (TryGetValue(key, out var value))
                    {
                        // [Careful]
                        // Early return is explicit.
                        // Fall-through cannot distinguish successful explicit null from a @throwable one.
                        return value;
                    }
                    else
                    {
                        var ePre = new NotifyCollectionChangingEventArgs(
                        action: NotifyCollectionChangingAction.Replace,
                            newItem: new DictionaryEntryPreview(key, value),
                            oldItem: new DictionaryEntryPreview(null!, null));

                        OnCollectionChanging(ePre);
                        return ePre.GetNewItemSingle<TValue>();
                    }
                }
            }
            set => base[key] = value;
        }

        [Careful("The target in this case is 'this' not '@base'.")]
        object? ITolerant.this[object key, bool @throw]
        {
            get
            {
                if (key is TKey keyT)
                {
                    return this[keyT];
                }
                else
                {
                    this.ThrowHard<InvalidCastException>(
                        $"Key must be of type {typeof(TKey).Name}, but was {key?.GetType().Name ?? "null"}.");
                    return null;
                }
            }
            set
            {
                if (key is not TKey keyT)
                {
                    this.ThrowHard<InvalidCastException>(
                        $"Key must be of type {typeof(TKey).Name}, but was {key?.GetType().Name ?? "null"}.");
                    return;
                }

                switch (value)
                {
                    case null:
                        this[keyT] = default!;
                        break;

                    case TValue valueT:
                        this[keyT] = valueT;
                        break;

                    default:
                        this.ThrowHard<InvalidCastException>(
                            $"Value must be of type {typeof(TValue).Name}, but was {value.GetType().Name}.");
                        break;
                }
            }
        }
    }
}
