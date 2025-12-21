using System.Collections;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    /// <summary>
    /// An ad-hoc tolerant dictionary.
    /// </summary>
    [Careful("Don't use TolerantDictionary. The eventing would be circular.")]
    public class ReverseLookup : Dictionary<IDictionary, BriskDictionaryWrapper>
    {
        public new BriskDictionaryWrapper? this[IDictionary key]
        {
            get
            {
                return TryGetValue(key, out var exists) ? exists : null;
            }
            internal set
            {
                if (value is not null)
                {
                    base[key] = value;
                }
            }
        }
    }
}
