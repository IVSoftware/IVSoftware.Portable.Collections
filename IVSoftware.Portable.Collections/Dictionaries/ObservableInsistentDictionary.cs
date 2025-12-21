using IVSoftware.Portable.Common.Exceptions;
using System.Collections;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    /// <summary>
    /// Dictionary variant that insists on key not found resolves to NotNull TValue.
    /// </summary>
    public class InsistentDictionary<TKey, TValue>
        : ObservableDictionary<TKey, TValue>
        , IInsistentDictionary<TKey, TValue>
        where TKey : notnull where TValue : notnull
    {
        public override TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out TValue? exists))
                {
                    return exists!; // Safe, because TValue is constrained to notnull,
                }
                TValue? preview = default;

                // INSISTENT
                // Additional heuristics:
                // 1. Try explicit activator dlgt that EUD has set.
                if (ActivationDlgt is not null && ActivationDlgt() is { } activated)
                {
                    preview = activated;
                }
                else
                {
                    ActivationDlgt = null; // Ensure that a non-successful dlgt is removed.
                    var tValue = typeof(TValue);
                    if (!tValue.IsAbstract && tValue.GetConstructor(Type.EmptyTypes) is { } ctor)
                    {
                        // 2. Try the parameterless CTor on TValue
                        var dlgt = () => Activator.CreateInstance<TValue>();
                        preview = dlgt();
                        if (preview is not null)
                        {
                            // Cache successful dlgt for next time.
                            ActivationDlgt = dlgt;
                        }
                    }
                    else if (typeof(TValue).TryActivateUnilateralContract<TValue?>(out TValue? uc))
                    {
                        // 3. A Unilateral contract (only succeeds on an interface not a class)
                        preview = uc;
                    }
                }

                var e = new NotifyCollectionChangingEventArgs(
                    action: NotifyCollectionChangingAction.Replace,
                    newItem: new DictionaryEntryPreview(key, preview),
                    oldItem: new DictionaryEntryPreview(key, null));
                OnCollectionChanging(e);

                if (e.Cancel)
                {
                    this.ThrowSoft<OperationCanceledException>();
                    return default!;
                }
                else
                {
                    if( e.GetNewItemSingle() is DictionaryEntryPreview entry && entry.Value is TValue valueT)
                    {
                        return valueT;
                    }
                    else
                    {
                        this.ThrowHard<InvalidOperationException>($"Contract violation.");
                        return default!; // We warned you...
                    }
                }
            }

#pragma warning disable CS8765
            set
            {
                if (value is null)
                {
                    this.ThrowHard<ArgumentNullException>(
                        "Insistent contract violation — attempted to assign null to TValue.");
                }
                else
                {
                    base[key] = value!;
                }
            }
#pragma warning restore CS8765
        }

        [Careful("The target in this case is 'this' not '@base'.")]
        object? IDictionary.this[object key]
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

        public Func<TValue>? ActivationDlgt { get; set; }
        Delegate? IInsistentDictionary.ActivationDlgt
        { 
            get => ActivationDlgt;
            set
            {
                if(value is Func<TValue> dlgt)
                {
                    ActivationDlgt = dlgt;
                }
            }
        }
    }
}
