using IVSoftware.Portable.Collections.Dictionaries;
using System.Collections.Specialized;

namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// Provides a central Brisk Dictionary for application-wide general caching.
    /// </summary>
    public static class Framework
    {
        static public IBriskDictionary Brisk
        {
            get
            {
                if (_brisk is null)
                {
                    _brisk = BriskReset();
                }
                return _brisk;
            }
        }
        static IBriskDictionary? _brisk = null;

        public static IBriskDictionary BriskReset()
            => ResetDlgt();

        /// <summary>
        /// Resets the library using the native default or a delegate injected by EUD.
        /// </summary>
        /// <remarks>
        /// TYPICAL USE - Reset point for unit tests that mess with the base config in one way or another.
        /// EXTREME USE - Full substitution of alternate implementation for <see cref="IBriskDictionary" />.
        /// </remarks>
        public static Func<IBriskDictionary> ResetDlgt
        {
            get => _resetDlgt ?? _resetDlgtDefault;
            set => _resetDlgtDefault = value;
        }
        static Func<IBriskDictionary>? _resetDlgt = null;
        static Func<IBriskDictionary> _resetDlgtDefault = () =>
        {
            if (_brisk is null)
            {
                _brisk = new BriskDictionary();
            }
            else
            {
                _brisk.Clear();
            }
            BriskDictionaryWrapper.Vacuum();
            return _brisk;
        };

        internal static void RaiseEvent(object? sender, EventArgs eUnk)
        {
            switch (eUnk)
            {
                case NotifyCollectionChangingEventArgs e:
                    CollectionChanging?.Invoke(sender, e);
                    break;
                case NotifyCollectionChangedEventArgs e:
                    CollectionChanged?.Invoke(sender, e);
                    break;
                default:
                    throw new NotImplementedException($"Bad case: {eUnk.GetType().FullName}");
            }
        }
        public static event NotifyCollectionChangingEventHandler? CollectionChanging;
        public static event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
}
