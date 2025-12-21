using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using System.Collections;
using System.Diagnostics;

namespace IVSoftware.Portable.Collections.Common
{
    [DebuggerDisplay("{SanityCount}")]
    sealed class Distinctifier : IEnumerable<object>
    {
        /// <summary>
        /// Provides resync and a cross-checking mechanism for debugging.
        /// </summary>
        /// <remarks>
        /// PARITY: "Agreement between two representations of the same concept."
        /// </remarks>
        private readonly IList @this;
        private readonly IObservablePreviewCollection? listOPC;
        public Distinctifier(IList @this)
        {
            this.@this = @this;
            listOPC = @this as IObservablePreviewCollection;
        }

        private readonly TolerantDictionary<object, int?> _histogram = new();
        public int SanityCount { get; private set; }

        private readonly object _lock = new();

        public static object NullKey { get; } = new object();

        public bool Add(object? key)
        {
            bool success;
            key ??= NullKey;
            lock (_lock)
            {
                if (_histogram[key] is null)
                {
                    _histogram[key] = 1;
                    success = true;
                }
                else
                {
                    _histogram[key]++;
                    success = false;
                }
            }
            SanityCount++;
            return success;
        }

        /// <summary>
        /// Attempt to remove or decrement the count of [key] in 
        /// the histogram return true if either op succeeds.
        /// </summary>
        public bool Remove(object? key)
        {
            key ??= NullKey;
            lock (_lock)
            {
                switch (_histogram[key])
                {
                    case null:
                        return false;
                    case 1:
                        _histogram.Remove(key); 
                        SanityCount--;
                        return true;
                    default:
                        _histogram[key]--; 
                        SanityCount--;
                        return true;
                }
            }
        }
        public void Clear()
        {
            lock (_lock)
            {
                _histogram.Clear();
                SanityCount = 0;
            }
        }
        public bool Contains(object? key)
        {
            this.OnAwaited();
            key ??= NullKey;
            lock (_lock)
            {
                return _histogram.ContainsKey(key);
            }
        }

        public IEnumerator<object> GetEnumerator()
            => _histogram.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_histogram.Keys).GetEnumerator();
        }

        public void SyncReset()
        {
            Clear();
            using (BeginAtomic())
            {
                lock (_lock)
                {
                    foreach (var item in @this)
                    {
                        var key = item ?? NullKey;

                        if (_histogram[key] is null)
                        {
                            _histogram[key] = 1;
                        }
                        else
                        {
                            _histogram[key]++;
                        }
                        SanityCount++;
                    }
                }
            }
        }

        public IDisposable BeginAtomic() => DHostAtomic.GetToken();

        /// <summary>
        /// Verifies the integrity of a Distinctifier transaction.
        /// </summary>
        /// <remarks> 
        /// 1. Distinctifier still operates even when cache mode is disabled, it
        ///    just doesn't refer to it for Contains which os O(n) in that case.
        /// 2. The thing we *really* disable is the error checking. If EUD ever 
        ///    runs into exceptions or issues with the optimization, we want to 
        ///    make sure that exceptions go away by turning off the optimization.
        /// </remarks>
        public DisposableHost DHostAtomic
        {
            get
            {
                if (_dhostTransaction is null)
                {
                    _dhostTransaction = new DisposableHost(nameof(DHostAtomic));
                    _dhostTransaction.FinalDispose += (sender, e) =>
                    {

                        if (@this is not null && DHostAtomic.IsZero())
                        {
                            // This is still O(1)...
                            if (@this.Count != SanityCount)
                            {
                                if (listOPC
                                    ?.OptimizationMode
                                    .HasFlag(Lists.ListOptimizationMode.UseCacheForContains) == true)
                                {
                                    this.ThrowFramework<InvalidOperationException>(
                                    $"Count is {@this.Count} but tallied count is {SanityCount}.");
                                }
                            }
#if DEBUG
                            // This is not.
                            if (_histogram.Values.OfType<int>().Sum(_ => _) != SanityCount)
                            {
                                this.ThrowFramework<InvalidOperationException>(
                                    $"Count is {@this.Count} but sum of count is {SanityCount}.");
                            }
#endif
                        }
                    };
                }
                return _dhostTransaction;
            }
        }
        DisposableHost? _dhostTransaction = null;
    }
}
