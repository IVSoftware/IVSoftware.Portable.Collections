using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using static IVSoftware.Portable.Collections.Framework;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{

    [Flags]
    public enum OCMCallFlag : uint
    {
        None = 0x0000,

        #region H I G H    O R D E R    B Y T E 
        Range = 0x1000,
        Distinct = Range << 1,
        Multiple = Distinct << 1,
        At = Multiple << 1,
        #endregion H I G H    O R D E R    B Y T E

        Add = 0x0001,

        Clear = Add << 1,

        /// <summary>
        /// Accessed using [Indexer]
        /// </summary>
        Replace = Clear << 1,

        Insert = Replace << 1,

        Move = Insert << 1,

        Remove = Move << 1,
        RemoveAt = Remove | At,
        NotSupported = Remove << 1,
    }

    [Flags]
    public enum OCMCall : uint
    {
        None = 0x0000,

        Add = OCMCallFlag.Add,

        AddRange = OCMCallFlag.Add | OCMCallFlag.Range,

        AddDistinct = OCMCallFlag.Add | OCMCallFlag.Distinct,

        AddRangeDistinct = OCMCallFlag.Add | OCMCallFlag.Range | OCMCallFlag.Distinct,

        Clear = OCMCallFlag.Clear,

        /// <summary>
        /// Accessed using [Indexer]
        /// </summary>
        Replace = OCMCallFlag.Replace,

        Insert = OCMCallFlag.Insert,

        InsertRange = OCMCallFlag.Insert | OCMCallFlag.Range,

        Move = OCMCallFlag.Move,

        Remove = OCMCallFlag.Remove,

        RemoveAt = OCMCallFlag.Remove | OCMCallFlag.At,

        RemoveRange = OCMCallFlag.Remove | OCMCallFlag.Range,

        RemoveMultiple = OCMCallFlag.Remove | OCMCallFlag.Multiple,

        NotSupported = OCMCallFlag.NotSupported,
    }

    public partial class ObservableCollectionMutator : IDisposable
    {
        [Canonical]
        private ObservableCollectionMutator(
            IList lut,
            bool enableRange,
            IList control,
            bool resetList,
            int? initialCount,
            int? seed = null)
        {
            SEED = seed ?? (new Random()).Next(); // Use an unseeded random if null and track it for injection if need be.
            Rando = new RandomOrNull(SEED);

            LUT = lut;
            CONTROL = control;
            IsRangeEnabled = enableRange;
            if (LUT is INotifyCollectionChanging inpcc)
            {
                inpcc.CollectionChanging += OnCollectionChanging;
            }
            if(LUT is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnCollectionChanged;

            }
            AwaitedEventArgs.Awaited += OnAwaited;
            Throw.BeginThrowOrAdvise += OnThrowOrAdvise;
            if(resetList)
            {
                using (DHostSuppress.GetToken())
                {
                    ResetList(initialCount);
                }
            }

            Framework.CollectionChanging += (sender, e) =>
            {
                if (sender is IDictionary dict)
                {
                    if(dict.Ancestors().OfType<Type>().FirstOrDefault() is { } type && 
                       type.IsAssignableTo(typeof(IList)))
                    {
                        MethodInfo mi;
                        switch (e.Action)
                        {
                            case NotifyCollectionChangingAction.Replace:
                                foreach (var entry in e.NewItems?.OfType<DictionaryEntryPreview>().Where(_ => _.Value is null) ?? [])
                                {
                                    switch (entry.Key)
                                    {
                                        case OCMCall.Move:
                                            mi = type.GetMethod(name: nameof(OCMCall.Move), types: [typeof(int), typeof(int)])!;
                                            entry.Value = mi;
                                            break;
                                        case OCMCall.Remove:
                                            mi = type.GetMethod("Remove", new[] { type.GetGenericArguments()[0] })!;
                                            entry.Value = mi;
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }
            };

            IDictionary dunk;

            // B R I S K    F O R    L U T    
            dunk = Brisk[LUT.GetType(), typeof(MethodInfo)].AsStronglyTypedDictionary<OCMCall, MethodInfo>(mode: DictionaryMode.InsistentNotNull);

            if (dunk.TryGetHost(out _))
            {   /* G T K */
            }
            else
            {
                throw new InvalidOperationException("Expecting Brisk On-Demand dunk is in the ReverseLookup now.");
            }

            // B R I S K    F O R    C O N T R O L  
            dunk = Brisk[CONTROL.GetType(), typeof(MethodInfo)].AsStronglyTypedDictionary<OCMCall, MethodInfo>(mode: DictionaryMode.InsistentNotNull);

            if (dunk.TryGetHost(out _))
            {   /* G T K */
            }
            else
            {
                throw new InvalidOperationException("Expecting Brisk On-Demand dunk is in the ReverseLookup now.");
            }
        }

        public ObservableCollectionMutator(
            IList lut, 
            bool enableRange,
            IList control, 
            int? initialCount, 
            int? seed = null)
            : this(lut, enableRange: enableRange, control, resetList: initialCount is not null, initialCount, seed) { }
        public ObservableCollectionMutator (
            IList lut,
            bool enableRange,
            IList control,
            bool resetList = true, 
            int? seed = null)
            : this(lut, enableRange: enableRange, control, resetList, null, seed) { }

        private void OnAwaited(object? sender, AwaitedEventArgs e)
        {
            var isDistinctifier = sender?.GetType().FullName == "IVSoftware.Portable.Collections.Common.Distinctifier";
            if (isDistinctifier)
            {
                if(LUTOPC?.OptimizationMode != ListOptimizationMode.UseCacheForContains)
                {
                    throw new InvalidOperationException("LUT should 'NOT' be invoking DistinctifierContains.");
                }
            }
            switch (e.Caller)
            {
                case nameof(IList.Contains):
                    if (LUTOPC?.OptimizationMode != ListOptimizationMode.UseCacheForContains)
                    {
                        throw new InvalidOperationException("LUT should 'NOT' be invoking DistinctifierContains.");
                    }
                    break;
            }
            if(isDistinctifier && e.Caller == nameof(IList.Contains))
            {
                CountDistinctifierContains++;
            }
        }

        bool IsCountAllowed
        {
            get
            {
                if(stim is null)
                {
                    return false;
                }
                if (LUT is ISuppressibleEventSource listS && listS.Suppressed != 0)
                {
                    return false;
                }
                if (!DHostSuppress.IsZero())
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Sets e.Cancel according to Cancel lottery OR injects error on demand.
        /// </summary>
        protected virtual void OnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
        {
            if (IsCountAllowed)
            {
                stim.Actual.CountChanging++;
                if (stim.IsCancelRequested)
                {
                    e.Cancel = true;
                }
                else
                {
                    if (stim.Validity == ValidityLottery.Responsive)
                    {
                        switch (stim.ErrorInjectFlag)
                        {
                            case 0:
                                throw new InvalidOperationException(
                                    $"{ValidityLottery.Responsive.ToFullKey()} must specify an error to induce.");
                            case ErrorInjectFlag.OldStartingIndex:
                                if (stim.Invalid.OldStartingIndex == -1)
                                {
                                    switch (e.Action)
                                    {
                                        case NotifyCollectionChangingAction.Add:
                                            // If you're here, this is an Insert not an Add
                                            // But now, it's indistinguishable from Add in ApplyChanges unless:
                                            stim.Invalid.OldStartingIndex = stim.GenerateIndexOutOfRange(allowMinusOne: false);
                                            break;
                                        case NotifyCollectionChangingAction.Remove:
                                            // Minus 1 won't be an error in ApplyChanges because the
                                            // algorithm simply removes object if it can find it.
                                            stim.Invalid.OldStartingIndex = stim.GenerateIndexOutOfRange(allowMinusOne: false);
                                            break;
                                        case NotifyCollectionChangingAction.Replace:
                                            // These always track in Replace mode.
                                            e.NewStartingIndex = e.OldStartingIndex;
                                            break;
                                    }
                                }
                                e.OldStartingIndex = stim.Invalid.OldStartingIndex;
                                break;
                            case ErrorInjectFlag.NewStartingIndex:
                                if (stim.Invalid.NewStartingIndex == -1)
                                {
                                    switch (e.Action)
                                    {
                                        // If you're here, this is an Insert not an Add
                                        case NotifyCollectionChangingAction.Add:
                                            // But now, it's indistinguishable from Add in ApplyChanges unless:
                                            stim.Invalid.NewStartingIndex = stim.GenerateIndexOutOfRange(allowMinusOne: false);
                                            break;
                                        case NotifyCollectionChangingAction.Replace:
                                            // These always track in Replace mode.
                                            e.OldStartingIndex = e.NewStartingIndex;
                                            break;
                                    }
                                }
                                e.NewStartingIndex = stim.Invalid.NewStartingIndex;
                                break;
                            case ErrorInjectFlag.NewItems:
                                e.NewItems = stim.Invalid.NewItems;
                                break;
                            case ErrorInjectFlag.OldItems:
                                e.OldItems = stim.Invalid.OldItems;
                                break;
                            case ErrorInjectFlag.Action:
                                throw new NotImplementedException("ToDo");
                                break;
                            default:
                                throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                        }
                    }
                }
            }
        }

        protected virtual void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsCountAllowed)
            {
                if (stim?.Validity == ValidityLottery.Responsive &&
                    LUT is not null &&
                    LUT is not INotifyCollectionChanging)
                {
                    stim.Expected.CountChanging = 0;
                    stim.Expected.CountChanged = 1;
                    stim.Expected.CountThrow = 0;
                }
                // Not an else
                stim.Actual.CountChanged++;
            }
        }

        protected virtual void OnThrowOrAdvise(object? sender, Throw e)
        {
            switch (e.Mode)
            {
                case ThrowOrAdvise.ThrowHard:
                    stim.Actual.CountThrow++;
                    break;
                case ThrowOrAdvise.ThrowSoft:
                    // More selective on these
                    if(e.Exception.GetType() == typeof(OperationCanceledException))
                    {
                        stim.Actual.CountThrow++;
                    }
                    break;
                case ThrowOrAdvise.ThrowFramework:
                    throw new InvalidOperationException("Framework Throw");
                case ThrowOrAdvise.Advisory:
                    break;
                default:
                    throw new NotImplementedException($"Bad case: {e.Mode.ToFullKey()}");
            }
            e.Handled = true;
        }

        const int DEFAULT_LIST_RESET_SIZE = 25;
        public static Type? @void { get; } = null;

        /// <summary>
        /// A null-capable Random that is always seeded.
        /// </summary>
        public RandomOrNull Rando = null!;
        public int ValidMAX { get; private set; }

        public static DisposableHost DHostSuppress { get; } = new DisposableHost(nameof(DHostSuppress));

        /// <summary>
        /// Pretty much the easiest way to rebuild this without events.
        /// </summary>
        public void ResetList(int? size = null)
        {
            size ??= DEFAULT_LIST_RESET_SIZE;
            var items = Enumerable
                .Range(1, (int)size)
                .Select(_ => Rando.Next(1, 201)).ToList();

            using(DHostSuppress.GetToken())
            {
                LUT.Clear();
                CONTROL.Clear();
                foreach (var item in items)
                {
                    LUT.Add(item);
                    CONTROL.Add(item);
                }
            }
            ValidMAX = LUT.OfType<int>().Max(_ => _);
        }
        public IList CONTROL { get; private set; }

        /// <summary>
        /// The List Under Test
        /// </summary>

        public IList LUT
        {
            get => _lut;
            private set
            {
                if (!Equals(_lut, value))
                {
                    _lut = value;
                    LUTR = value as IRangeable;
                    LUTOPC = value as IObservablePreviewCollection;
                }
            }
        }
        IList _lut = default!;
        public IRangeable? LUTR { get; private set; }
        public IObservablePreviewCollection? LUTOPC { get; private set; }

        /// <summary>
        /// Needs intention + wherewithal.
        /// </summary>
        public bool IsRangeEnabled
        {
            get => _isRangeEnabled && LUTR is not null;
            set => _isRangeEnabled = value;
        }
        bool _isRangeEnabled = false;

        public int LoopIndex { get; internal set; }
        public int SEED { get; }
        public int CountDistinctifierContains { get; internal set; }

        private MutatorInstance stim = null!;

        public MutatorInstance RunMutation(bool stopOn)
        {
            if(stopOn)
            { }

            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    if(stim is not null)
                    {
                        throw new InvalidOperationException($"{nameof(stim)} must be null");
                    }
                    else
                    {
                        stim = new MutatorInstance(this, stopOn);
                    }
                },
                onDispose: (sender, e) =>
                {
                    stim = null!;
                });
            int lottery;
            OCMCall ocmCall = 0;

            lottery = Rando.NextNotNull(1, 10, inclusive: true);
            switch (lottery)
            {
                case 1: ocmCall |= OCMCall.Clear; break;
                case 2: ocmCall |= OCMCall.Add; break;
                case 3: ocmCall |= OCMCall.Add; break;
                case 4: ocmCall |= OCMCall.Add; break;
                case 5: ocmCall |= OCMCall.Replace; break;
                case 6: ocmCall |= OCMCall.Insert; break;
                case 7: ocmCall |= OCMCall.Insert; break;
                case 8: ocmCall |= OCMCall.Move; break;
                case 9: ocmCall |= OCMCall.Remove; break;
                case 10: ocmCall |= OCMCall.Remove; break;
                default: throw new NotImplementedException($"Bad case: {lottery}");
            }
            switch (ocmCall)
            {
                case OCMCall.Add:
                    lottery = Rando.NextNotNull(1, 4, inclusive: true);
                    switch (lottery)
                    {
                        case 1: break;
                        case 2:
                            ocmCall |= (OCMCall)OCMCallFlag.Distinct; break;
                        case 3:
                            if(IsRangeEnabled) ocmCall |= (OCMCall)OCMCallFlag.Range; break;
                        case 4:
                            if(IsRangeEnabled) ocmCall |= (OCMCall)((OCMCallFlag.Distinct | OCMCallFlag.Range)); 
                            break;
                        default: throw new NotImplementedException($"Bad case: {lottery}");
                    }
                    break;
                case OCMCall.Clear:
                    break;
                case OCMCall.Replace:
                    break;
                case OCMCall.Insert:
                    lottery = Rando.NextNotNull(1, 2, inclusive: true);
                    switch (lottery)
                    {
                        case 1: break; 
                        case 2: if(IsRangeEnabled) ocmCall |= (OCMCall)OCMCallFlag.Range; break; 
                        default: throw new NotImplementedException($"Bad case: {lottery}");
                    }
                    break;
                case OCMCall.Move:
                    break;
                case OCMCall.Remove:
                    if(IsRangeEnabled)
                    { 
                        lottery = Rando.NextNotNull(1, 4, inclusive: true);
                        switch (lottery)
                        {
                            case 1: break;
                            case 2: ocmCall |= (OCMCall)OCMCallFlag.At; break;
                            case 3: ocmCall |= (OCMCall)OCMCallFlag.Range; break;
                            case 4: ocmCall |= (OCMCall)OCMCallFlag.Multiple; break;
                            default: throw new NotImplementedException($"Bad case: {lottery}");
                        }
                    }
                    else
                    {
                        lottery = Rando.NextNotNull(1, 2, inclusive: true);
                        switch (lottery)
                        {
                            case 1: break;
                            case 2: ocmCall |= (OCMCall)OCMCallFlag.At; break;
                            default: throw new NotImplementedException($"Bad case: {lottery}");
                        }
                    }
                    break;
                default: throw new NotImplementedException($"Bad case: {ocmCall}");
            }

            //Debug.Assert(DateTime.Now.Date == new DateTime(2025, 11, 23).Date, "Don't forget disabled");
            //ocmCall = OCMCallFlag.Add;

            stim.InitializeOCMCall(ocmCall);
            switch (ocmCall)
            {
                case OCMCall.Add:
                    OnAdd(stim);
                    break;

                case OCMCall.AddRange:
                    OnAddRange(stim);
                    break;

                case OCMCall.AddDistinct:
                    OnAddDistinct(stim);
                    break;

                case OCMCall.AddRangeDistinct:
                    OnAddRangeDistinct(stim);
                    break;

                case OCMCall.Clear:
                    OnClear(stim);
                    break;

                case OCMCall.Replace:
                    OnReplace(stim);
                    break;

                case OCMCall.Insert:
                    OnInsert(stim);
                    break;

                case OCMCall.InsertRange:
                    OnInsertRange(stim);
                    break;

                case OCMCall.Move:
                    OnMove(stim);
                    break;

                case OCMCall.Remove:
                    OnRemove(stim);
                    break;

                case OCMCall.RemoveAt:
                    OnRemoveAt(stim);
                    break;

                case OCMCall.RemoveRange:
                    OnRemoveRange(stim);
                    break;

                case OCMCall.RemoveMultiple:
                    OnRemoveMultiple(stim);
                    break;

                default:
                    throw new NotImplementedException($"Bad case: {ocmCall}");
            }
            stim.Actual.SerializeResult(LUT);
            return stim;
        }

        protected virtual void OnAdd(MutatorInstance stim)
        {

            if (stim.Validity == ValidityLottery.Preemptive)
            {
                stim.Actual.Result = LUT.Add(stim.Invalid.GetNewItemSingle());
            }
            else
            {
                // [Canonical]
                if(LUT is INotifyCollectionChanging || !stim.IsCancelRequested)
                {
                    stim.Actual.Result = LUT.Add(stim.Valid.GetNewItemSingle());
                }
                else 
                {
                    stim.Actual.Result = stim.Expected.Result;
                }
            }
        }

        protected virtual void OnAddRange(MutatorInstance stim)
        {
            var lutR = (IRangeable)LUT;
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    case ErrorInjectFlag.NewItems:
                        lutR.AddRange(stim.Invalid.NewItems!);
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                }
            }
            else
            {
                lutR.AddRange(stim.Valid.NewItems!);
            }
        }

        protected virtual void OnAddDistinct(MutatorInstance stim)
        {
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                stim.Actual.Result = LUTOPC!.AddDistinct(stim.Invalid.NewItems![0]!);
            }
            else
            {
                stim.Actual.Result = LUTOPC!.AddDistinct(stim.Valid.NewItems![0]!);
            }
        }

        protected virtual void OnAddRangeDistinct(MutatorInstance stim)
        {
            var lutRD = (IObservablePreviewCollection)LUT;
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                stim.Actual.Result = lutRD.AddRangeDistinct(stim.Invalid.NewItems!);
            }
            else
            {
                stim.Actual.Result = lutRD.AddRangeDistinct(stim.Valid.NewItems!);
            }
        }

        protected virtual void OnClear(MutatorInstance stim)
        {
            LUT.Clear();
        }

        protected virtual void OnReplace(MutatorInstance stim)
        {
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    case ErrorInjectFlag.NewStartingIndex:
                        LUT[stim.Invalid.NewStartingIndex] = stim.Valid.NewItems![0];
                        break;
                    case ErrorInjectFlag.OldStartingIndex:
                        LUT[stim.Invalid.OldStartingIndex] = stim.Valid.NewItems![0];
                        break;
                    case ErrorInjectFlag.NewItems:
                        LUT[stim.Valid.NewStartingIndex] = stim.Invalid.NewItems![0];
                        break;
                    case ErrorInjectFlag.OldItems:
                        LUT[stim.Valid.OldStartingIndex] = stim.Invalid.OldItems![0];
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                }
            }
            else
            {
                // REPLACE using Indexer.
                LUT[stim.Valid.NewStartingIndex] = stim.Valid.NewItems![0];
            }
        }

        protected virtual void OnInsert(MutatorInstance stim)
        {
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    case ErrorInjectFlag.NewStartingIndex:
                        LUT.Insert(stim.Invalid.NewStartingIndex, stim.Valid.NewItems![0]);
                        break;
                    case ErrorInjectFlag.NewItems:
                        LUT.Insert(stim.Valid.NewStartingIndex, stim.Invalid.NewItems![0]);
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                }
            }
            else
            {
                LUT.Insert(stim.Valid.NewStartingIndex, stim.Valid.NewItems![0]);
            }
        }

        protected virtual void OnInsertRange(MutatorInstance stim)
        {
            var lutR = (IRangeable)LUT;
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    case ErrorInjectFlag.NewStartingIndex:
                        LUTOPC!.InsertRange(stim.Invalid.NewStartingIndex, stim.Valid.NewItems!);
                        break;
                    case ErrorInjectFlag.NewItems:
                        LUTOPC!.InsertRange(stim.Valid.NewStartingIndex, stim.Invalid.NewItems!);
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                        break;
                }
            }
            else
            {
                lutR.InsertRange(stim.Valid.NewStartingIndex, stim.Valid.NewItems!);
            }
        }

        protected virtual void OnMove(MutatorInstance stim)
        {
            switch (stim.Validity)
            {
                case ValidityLottery.Valid:
                    // Exec move WITH inline here and now checking.
                    var itemB4 = LUT[stim.Valid.OldStartingIndex];
                    LUTOPC!.Move(stim.Valid.OldStartingIndex, stim.Valid.NewStartingIndex);
                    var itemFTR = LUT[stim.Valid.NewStartingIndex];
                    if (!stim.IsCancelRequested)
                    {
                        Assert.AreEqual(itemB4, itemFTR);
                    }
                    break;
                case ValidityLottery.Preemptive:
                    switch (stim.ErrorInjectFlag)
                    {
                        case 0:
                            throw new InvalidOperationException(
                                $"{ValidityLottery.Responsive.ToFullKey()} must specify an error to induce.");
                        case ErrorInjectFlag.NewStartingIndex:
                            LUTOPC!.Move(stim.Valid.OldStartingIndex, stim.Invalid.NewStartingIndex);
                            break;
                        case ErrorInjectFlag.OldStartingIndex:
                            LUTOPC!.Move(stim.Invalid.OldStartingIndex, stim.Valid.NewStartingIndex);
                            break;
                        case ErrorInjectFlag.Action:
                            throw new NotImplementedException("ToDo");
                            break;
                        case ErrorInjectFlag.NewItems:
                        case ErrorInjectFlag.OldItems:
                        default:
                            throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                            break;
                    }
                    break;
                case ValidityLottery.Responsive:
                    // Exec move WITHOUT inline here and now checking (anticipating that
                    // an error will be introduced in the CollectionChanging handler).
                    LUTOPC!.Move(stim.Valid.OldStartingIndex, stim.Valid.NewStartingIndex);
                    break;
                default:
                    break;
            }
        }


        protected virtual void OnRemove(MutatorInstance stim)
        {
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    default:
                        throw new NotImplementedException(
                            $"Bad case: {stim.ErrorInjectFlag.ToFullKey()} cannot error.");
                }
            }
            else
            {
                stim.Actual.Result = InvokeRemove(LUT, stim.Valid.OldItems![0]);
            }
        }

        protected virtual void OnRemoveAt(MutatorInstance stim)
        {
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                LUT.RemoveAt(stim.Invalid.OldStartingIndex);
            }
            else
            {
                LUT.RemoveAt(stim.Valid.OldStartingIndex);
            }
        }

        protected virtual void OnRemoveRange(MutatorInstance stim)
        {
            CollectionRange range;
            var lutR = (IRangeable)LUT;
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    case ErrorInjectFlag.OldItems:
                        range = stim.Invalid.OldItems!.Cast<CollectionRange>().Single();
                        lutR.RemoveRange(range.StartIndex, range.EndIndex);
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                        break;
                }
            }
            else
            {
                range = stim.Valid.OldItems!.Cast<CollectionRange>().Single();
                lutR.RemoveRange(range.StartIndex, range.EndIndex);
            }
        }

        protected virtual void OnRemoveMultiple(MutatorInstance stim)
        {
            var lutR = (IRangeable)LUT;
            if (stim.Validity == ValidityLottery.Preemptive)
            {
                switch (stim.ErrorInjectFlag)
                {
                    case ErrorInjectFlag.OldItems:
                        stim.Actual.Result = lutR.RemoveMultiple(stim.Invalid.OldItems!);
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {stim.ErrorInjectFlag.ToFullKey()}");
                        break;
                }
            }
            else
            {
                stim.Actual.Result = lutR.RemoveMultiple(stim.Valid.OldItems!);
            }
        }

        public void Dispose()
        {
            if (LUT is INotifyCollectionChanging inpcc)
            {
                inpcc.CollectionChanging -= OnCollectionChanging;
            }
            if (LUT is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= OnCollectionChanged;
            }
            Throw.BeginThrowOrAdvise -= OnThrowOrAdvise;
            AwaitedEventArgs.Awaited -= OnAwaited;
        }

        public void EnsureNotEmptyCONTROL()
        {
            if (CONTROL.Count == 0)
            {
                ResetList();
            }
        }
    }
}
