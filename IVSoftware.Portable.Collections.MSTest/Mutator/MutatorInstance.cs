using IVSoftware.Portable.Collections.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Diagnostics;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{
    public partial class ObservableCollectionMutator
    {

        public class MutatorInstance
        {
            public MutatorInstance(ObservableCollectionMutator mut, bool stopOn)
            {
                if (stopOn)
                { }

                MUT = mut;

                _stopOn = stopOn;
                LoopIndex = MUT.LoopIndex;
                SEED = MUT.SEED;
                MUT.EnsureNotEmptyCONTROL();

                // Create empty.
                Expected = new OCMMutationCompare(MutationPhase.Expected);
                Actual = new OCMMutationCompare(MutationPhase.Actual);
                InitialState = JsonConvert.SerializeObject(MUT.LUT);
            }
            public void InitializeOCMCall(OCMCall ocmCall)
            {
                OCMCall = ocmCall;

                var actionLottery = (NotifyCollectionChangingAction)MUT.Rando.NextNotNull(5);
                Valid = new StimPreview(ocmCall);
                Invalid = new StimPreview(ocmCall);

                IsCancelRequested = MUT.Rando.Next(11) == 10;   // One in 10 chance of cancel op.

                // Run the validity lottery.
                if (IsCancelRequested)
                {
                    // NO chance of Responsive, because IsCancelRequested is checked first.
                    Validity =
                            MUT.Rando.NextNotNull(10, inclusive: true) != 0
                            ? ValidityLottery.Valid         // 90% probability of overall valid
                            : ValidityLottery.Preemptive;   // 10% probability of preemptive
                }
                else
                {
                    Validity =
                            MUT.Rando.NextNotNull(10, inclusive: true) != 0
                            ? ValidityLottery.Valid                 // 90% probability of overall valid
                            : MUT.Rando.NextNotNull(1, inclusive: true) == 0
                                ? ValidityLottery.Preemptive        // 5% probability of preemptive
                                : ValidityLottery.Responsive;       // 5% probability of responsive
                }

                // Throw if somehow this is null!
                var itemType = MUT.LUT.GetType().GetGenericArguments()[0];

                if (_stopOn)
                {
                    // Set up the limits.
                }
                switch (ocmCall)
                {
                    case OCMCall.Add:
                        Valid.NewItems = localGenerateItemsToAddOrInsert(itemType, itemsCount: 1);
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        if (SetOnCanceledOrInvalid(-1))
                        {
                            // No lottery - one choice only
                            ErrorInjectFlag = ErrorInjectFlag.NewItems;
                        }
                        else
                        {
                            SetExpectedSuccessResult(MUT.CONTROL.Add(Valid.NewItems![0]));
                        }
                        break;
                    case OCMCall.AddDistinct:
                        Valid.NewItems = localGenerateItemsToAddOrInsert(itemType, itemsCount: 1);
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);

                        switch (this.MockAddDistinct(Valid.NewItems![0], preview: true))
                        {
                            case true:
                                if (IsCancelRequested)
                                {
                                    Expected.Result = false;
                                    Expected.CountChanging = 1;
                                    Expected.CountChanged = 0;
                                    Expected.CountThrow = 1;
                                }
                                else
                                {
                                    switch (Validity)
                                    {
                                        case ValidityLottery.Valid:
                                            Expected.Result = true;
                                            Expected.CountChanging = 1;
                                            Expected.CountChanged = 1;
                                            Expected.CountThrow = 0;
                                            if (MockAddDistinct(Valid.NewItems![0], preview: false) != true)
                                            {
                                                throw new InvalidOperationException("Expecting SUCCESS because this op has been vetted.");
                                            }
                                            break;
                                        case ValidityLottery.Preemptive:
                                            Expected.Result = false;
                                            Expected.CountChanging = 0;
                                            Expected.CountChanged = 0;
                                            Expected.CountThrow = 1;
                                            break;
                                        case ValidityLottery.Responsive:
                                            Expected.Result = false;
                                            Expected.CountChanging = 1;
                                            Expected.CountChanged = 0;
                                            Expected.CountThrow = 1;
                                            ErrorInjectFlag = ErrorInjectFlag.NewItems;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                break;
                            case false:
                                Expected.Result = false;
                                // Detection of non-distinct is preemptive and
                                // no opportunity to cancel will be provided.
                                Expected.CountChanging = 0;
                                Expected.CountChanged = 0;
                                Expected.CountThrow = 0;
                                break;
                            case null:
                                Expected.Result = false;
                                // Detection of invalid cast is preemptive and
                                // no opportunity to cancel will be provided.
                                Expected.CountChanging = 0;
                                Expected.CountChanged = 0;
                                Expected.CountThrow = 1;
                                break;
                        }
                        // In this case the preload is complicated enough
                        // that we just debrief at the end.
                        if(Validity == ValidityLottery.Preemptive)
                        {
                            // No lottery - one choice only
                            ErrorInjectFlag = ErrorInjectFlag.NewItems;
                            Expected.CountChanging = 0;
                            Expected.CountChanged = 0;
                            Expected.CountThrow = 1;
                        }
                        break;
                    case OCMCall.AddRange:
                        Valid.NewItems = localGenerateItemsToAddOrInsert(itemType, itemsCount: 1);
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        if (SetOnCanceledOrInvalid(@void))
                        {
                            // No lottery - one choice only
                            ErrorInjectFlag = ErrorInjectFlag.NewItems;
                        }
                        else
                        {
                            foreach (var item in Valid.NewItems!)
                            {
                                MUT.CONTROL.Add(item);
                            }
                            // This isn't like AddDistinct. There's nothing
                            // to report interms of conditional success.
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.AddRangeDistinct:
                        Valid.NewItems = localGenerateItemsToAddOrInsert(itemType);
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid));
                        if (SetOnCanceledOrInvalid(0))
                        {
                            // No lottery - one choice only
                            ErrorInjectFlag = ErrorInjectFlag.NewItems;
                        }
                        else
                        {
                            int success = 0;
                            foreach (var item in Valid.NewItems!)
                            {
                                if (!MUT.CONTROL.Contains(item))
                                {
                                    MUT.CONTROL.Add(item);
                                    success++;
                                }
                            }
                            SetExpectedSuccessResult(success);
                        }
                        break;
                    case OCMCall.Clear:
                        // This model broadly assumes that the Clear method cannot be corrupted.
                        Validity = ValidityLottery.Valid;
                        if (!SetOnCanceledOrInvalid(@void))
                        {
                            MUT.CONTROL.Clear();
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.Replace:
                        Valid.NewStartingIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);
                        Valid.OldStartingIndex = Valid.NewStartingIndex;    // Tracks always

                        Valid.OldItems = localGenerateItemsToAddOrInsert(itemType, itemsCount: 1);
                        Valid.NewItems = Valid.OldItems;

                        // Do NOT allow minus one because Replace is tolerant of
                        // that and just finds by item, subverting the test.
                        Invalid.NewStartingIndex = GenerateIndexOutOfRange();
                        Invalid.OldStartingIndex = Invalid.NewStartingIndex; // Tracks always, even if invalid.

                        Invalid.OldItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);

                        if (SetOnCanceledOrInvalid(@void))
                        {
                            RunErrorLottery(
                                ErrorInjectFlag.NewStartingIndex 
                                | ErrorInjectFlag.OldStartingIndex 
                                | ErrorInjectFlag.NewItems
                                | ErrorInjectFlag.OldItems);
                        }
                        else
                        {
                            MUT.CONTROL[Valid.NewStartingIndex] = Valid.NewItems![0];
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.Insert:
                        Valid.NewStartingIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);
                        Valid.NewItems = localGenerateItemsToAddOrInsert(itemType, itemsCount: 1);

                        // Do NOT allow minus one, because the Insert becomes an Add in that case.
                        Invalid.NewStartingIndex = GenerateIndexOutOfRange();
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        if (SetOnCanceledOrInvalid(@void))
                        {
                            RunErrorLottery(ErrorInjectFlag.NewStartingIndex | ErrorInjectFlag.NewItems);
                        }
                        else
                        {
                            MUT.CONTROL.Insert(Valid.NewStartingIndex, Valid.NewItems![0]);
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.InsertRange:
                        Valid.NewItems = localGenerateItemsToAddOrInsert(itemType);
                        Valid.NewStartingIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);

                        // Do NOT allow minus one, because the Insert becomes an Add in that case.
                        Invalid.NewStartingIndex = GenerateIndexOutOfRange();
                        Invalid.NewItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        if (SetOnCanceledOrInvalid(@void))
                        {
                            RunErrorLottery(ErrorInjectFlag.NewStartingIndex | ErrorInjectFlag.NewItems);
                        }
                        else
                        {
                            for (int i = 0; i < Valid.NewItems!.Count; i++)
                            {
                                MUT.CONTROL.Insert(Valid.NewStartingIndex + i, Valid.NewItems![i]);
                            }
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.Move:
                        Valid.OldStartingIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);
                        Valid.NewStartingIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);

                        Invalid.NewStartingIndex = GenerateIndexOutOfRange();
                        Invalid.OldStartingIndex = GenerateIndexOutOfRange();

                        if (SetOnCanceledOrInvalid(@void))
                        {
                            RunErrorLottery(ErrorInjectFlag.NewStartingIndex | ErrorInjectFlag.OldStartingIndex);
                        }
                        else
                        {
                            InvokeMove(MUT.CONTROL, Valid.OldStartingIndex, Valid.NewStartingIndex);
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.Remove:
                        // This is *always* a single item function. The lottery
                        // determines whether the item is in list when removed.
                        var isItemInListLottery = MUT.Rando.NextNotNull(1, inclusive: true) == 0;
                        Valid.OldItems =
                            isItemInListLottery
                            ? localGenerateItemsToRemove(itemType, itemsInListCount: 1, itemsNotInListCount: 0)
                            : localGenerateItemsToRemove(itemType, itemsInListCount: 0, itemsNotInListCount: 1);
                        // NOTE: Running the normal error lottery is NA here.
                        Validity = ValidityLottery.Valid;
                        if (!SetOnCanceledOrInvalid(false))
                        {
                            MUT.CONTROL.Remove(Valid.OldItems![0]);
                            SetExpectedSuccessResult(isItemInListLottery);
                        }
                        break;
                    case OCMCall.RemoveAt:
                        Valid.OldItems = localGenerateItemsToAddOrInsert(itemType, itemsCount: 1);
                        Valid.OldStartingIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);

                        Invalid.OldItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        Invalid.OldStartingIndex = GenerateIndexOutOfRange();
                        if (SetOnCanceledOrInvalid(@void))
                        {
                            RunErrorLottery(ErrorInjectFlag.OldStartingIndex | ErrorInjectFlag.OldItems);
                        }
                        else
                        {
                            MUT.CONTROL.RemoveAt(Valid.OldStartingIndex);
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.RemoveRange:
                        var validStartIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);
                        var validEndIndex = MUT.Rando.NextNotNull(MUT.LUT.Count);
                        Valid.OldItems = new List<CollectionRange>([new CollectionRange(validStartIndex, validEndIndex)]);

                        
                        var invalidStartIndex = GenerateIndexOutOfRange();
                        var invalidEndIndex = GenerateIndexOutOfRange();
                        Invalid.OldItems = new List<CollectionRange>([new CollectionRange(invalidStartIndex, invalidEndIndex)]);

                        if (SetOnCanceledOrInvalid(@void))
                        {
                            // The error injection is still singular, because
                            // the DTO now contains invalid start or end index.
                            ErrorInjectFlag = ErrorInjectFlag.OldItems;
                        }
                        else
                        {
                            var range = Valid.OldItems!.Cast<CollectionRange>().Single();
                            for (int i = 0; i < range.Count; i++)
                            {
                                MUT.CONTROL.RemoveAt(range.StartIndex);
                            }
                            SetExpectedSuccessResult(@void);
                        }
                        break;
                    case OCMCall.RemoveMultiple:
                        Valid.OldItems = localGenerateItemsToAddOrInsert(itemType);
                        Invalid.OldItems = localGenerateItemsToAddOrInsert(typeof(Guid), itemsCount: 1);
                        if (SetOnCanceledOrInvalid(0))
                        {
                            ErrorInjectFlag = ErrorInjectFlag.OldItems;
                        }
                        else
                        {
                            int success = 0;
                            foreach (var item in Valid.OldItems!)
                            {
                                if (MUT.CONTROL.Contains(item))
                                {
                                    MUT.CONTROL.Remove(item);
                                    success++;
                                }
                            }
                            SetExpectedSuccessResult(success);
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Bad case: {ocmCall}.ToFullKey()");
                }
                Expected.SerializeResult(MUT.CONTROL);

                /// <summary>
                /// If itemsCount is null generates between 1 and 5 items on
                /// a random basis. Otherwise generates exactly N items.
                /// </summary>
                IList? localGenerateItemsToAddOrInsert(Type type, int? itemsCount = null)
                {
                    itemsCount ??= MUT.Rando.NextNotNull(1, 5, inclusive: true);
                    var items = new List<object?>();
                    for (int i = 0; i < itemsCount; i++)
                    {
                        var e = new BeforeItemCreateEventArgs(type, MUT.Rando, index: i);
                        BeforeItemCreate?.Invoke(MUT.LUT, e);
                        items.Add(e.Item);
                    }
                    return items;
                }

                IList? localGenerateItemsToRemove(Type type, int? itemsInListCount, int? itemsNotInListCount)
                {
                    itemsInListCount ??= MUT.Rando.NextNotNull(1, 5, inclusive: true);

                    // Prevent infinite (because of hash).
                    if (itemsInListCount > MUT.LUT.Count)
                    {
                        itemsInListCount = MUT.LUT.Count;
                    }

                    var items = new List<object?>();
                    var visited = new HashSet<int>();
                    for (int i = 0; i < itemsInListCount; i++)
                    {
                        var guard = 0;
                        while (++guard <= 100)
                        {
                            // Random in-range index.
                            int indexOfValidItem = MUT.Rando.NextNotNull(MUT.LUT.Count);
                            // Distinctify
                            if (visited.Add(indexOfValidItem))
                            {
                                items.Add(MUT.LUT[indexOfValidItem]);
                                break;
                            }
                        }
                    }

                    // Heavily weighted to 0 count in random mode
                    itemsNotInListCount ??= Math.Max(0, MUT.Rando.NextNotNull(-5, 5, inclusive: true));
                    int retry, maxRetry = 100;
                    for (int i = 0; i < itemsNotInListCount; i++)
                    {
                        retry = 0;
                        while(retry++ < maxRetry)
                        {
                            var e = new BeforeItemCreateEventArgs(type, MUT.Rando, index: i);
                            BeforeItemCreate?.Invoke(MUT.LUT, e);
                            if (!MUT.LUT.Contains(e.Item))
                            {
                                items.Add(e.Item);
                                break;
                            }
                        }
                    }
                    return items;
                }
            }

            /// <summary>
            /// Allow generation of -1 for preemptive, then modify in the 
            /// responsive block as needed using allowMinusOne: false.
            /// </summary>
            internal int GenerateIndexOutOfRange(bool allowMinusOne = true)
            {
                // - Increment the Count by one to get a minval that is OOR for any action.
                // - Otherwise, something like Insert @ Count just turns into an Add.
                int
                    minVal = MUT.CONTROL.Count + 1,
                    maxVal = minVal + byte.MaxValue;
                if (allowMinusOne)
                {
                    return MUT.Rando.NextNotNull(2, inclusive: true) == 0
                    ? -1
                    : MUT.Rando.NextNotNull(minVal, maxVal);
                }
                else
                {
                    return MUT.Rando.NextNotNull(minVal, maxVal);
                }
            }

            /// <summary>
            /// Returns true if added, false if not, and null for invalid cast.
            /// </summary>
            private bool? MockAddDistinct(object? item, bool preview)
            {
                if (MUT.CONTROL.Contains(item))
                {
                    return false;
                }
                else
                {
                    var Tvalue = MUT.LUT.GetType().GetGenericArguments()[0];
                    if (item.IsAssignableAs(Tvalue))
                    {
                        if (!preview)
                        {
                            MUT.CONTROL.Add(item);
                        }
                        return true;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public event EventHandler<BeforeItemCreateEventArgs>? BeforeItemCreate;

            private readonly ObservableCollectionMutator MUT;

            private readonly bool _stopOn;

            public int LoopIndex { get; }
            public int SEED { get; }
            public int CountDistinctifierContains { get; internal set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public OCMCall OCMCall { get; private set; } = 0;
            public void SetCallNotSupported()
            {
                OCMCall |= (OCMCall)OCMCallFlag.NotSupported;
                Expected.CountChanging = 0;
                Expected.CountChanged = 0;
                Expected.CountThrow = 0;
            }

            /// <summary>
            /// The method is a non-starter, like a distinct that offers no distincts.
            /// </summary>
            public bool IsValidNOOP { get; }

            /// <summary>
            /// The untouched incoming list.
            /// </summary>
            public string InitialState { get; }

            /// <summary>
            /// Indicates that the test fixture should induce an error, either
            /// as preemptive bad data going into the method OR as a simulated
            /// problem with the EUD collection changing handler.
            /// </summary>
            [JsonConverter(typeof(StringEnumConverter))]
            public ValidityLottery Validity { get; private set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public ErrorInjectFlag ErrorInjectFlag { get; private set; } = 0;
            public bool IsCancelRequested { get; private set; }
            public string? ExpectedException { get; private set; }

            public StimPreview Valid { get; private set; } = null!;

            public StimPreview Invalid
            {
                get
                {
                    if (_invalid is null)
                    {
                        _invalid = new StimPreview(Valid, null);
                    }
                    return _invalid;
                }
                private set
                {
                    _invalid = value;
                }
            }
            StimPreview? _invalid = null;

            public OCMMutationCompare Expected { get; }
            public OCMMutationCompare Actual { get; }

            /// <summary>
            /// If more than one type of error is possible, chose one of them at random.
            /// </summary>
            /// <remarks>
            /// When Validity is preemptive, the error is preeminent. Otherwise, when 
            /// error is responsive and cancel is requested, cancel is preeminent. 
            /// </remarks>
            public void RunErrorLottery(ErrorInjectFlag allowed, bool autoIncludeAction = false)
            {
                if(allowed == 0)
                {
                    throw new ArgumentOutOfRangeException("Guaranteed by design to have one or more flags.");
                }
                if (autoIncludeAction)
                {
                    allowed |= ErrorInjectFlag.Action;
                }
                bool hasNewStartingIndex = false;
                ErrorInjectFlag[] lottery =
                    Enum
                    .GetValues<ErrorInjectFlag>()
                    .Where(_ => allowed.HasFlag(_))
                    .ToArray();
                { }
                switch (lottery.Length)
                {
                    // Repesents a case like Reset that is impossible to error on.
                    case 0:
                        throw new InvalidOperationException("Guaranteed by design to have one or more flags.");
                    case 1:
                        ErrorInjectFlag = lottery[0];
                        break;
                    default:
                        int index;
                        if (lottery.Contains(ErrorInjectFlag.NewStartingIndex))
                        {
                            // Get WEIGHTED result.
                            int min = hasNewStartingIndex ? -lottery.Length : 0;
                            index = MUT.Rando.NextNotNull(min, lottery.Length);
                            if (index < 0)
                            {
                                // 5x as likely than any other flag.
                                ErrorInjectFlag = ErrorInjectFlag.NewStartingIndex;
                            }
                            else
                            {
                                ErrorInjectFlag = lottery[index];
                            }
                        }
                        else
                        {
                            index = MUT.Rando.NextNotNull(lottery.Length);
                            ErrorInjectFlag = lottery[index];
                        }
                        break;
                }
            }

            /// <summary>
            /// Return true on Cancel or Error
            /// </summary>
            [Canonical]
            public bool SetOnCanceledOrInvalid(object? resultCancelOrNoop)
            {
                if (IsValidNOOP)
                {
                    IsCancelRequested = false;
                    // The signature of NOOP is like nothing ever happened.
                    Expected.Set(
                        result: resultCancelOrNoop,
                        countChanging: 0,
                        countChanged: 0,
                        countThrow: 0);
                    return true; // Benign cancellation - nothing to see here.
                }
                else
                {
                    // Note that we can't run a validity lottery in the general case.
                    switch (Validity)
                    {
                        case ValidityLottery.Valid:
                            if (IsCancelRequested)
                            {
                                if (MUT.LUT is INotifyCollectionChanging)
                                {
                                    // When Cancel is set in the PreviewCollectionChanging
                                    // handler, it causes a throw for OperationCanceled.
                                    Expected.Set(
                                        result: resultCancelOrNoop,
                                        countChanging: 1,
                                        countChanged: 0,
                                        countThrow: 1);

                                    ExpectedException = nameof(OperationCanceledException);
                                }
                                else
                                {
                                    // INCC ONLY (ObservableCollection<T>
                                    Expected.Set(
                                        result: resultCancelOrNoop,
                                        countChanging: 0,   // No event
                                        countChanged: 0,
                                        countThrow: 0);     // No cancelation exception
                                }
                                return true; // Benign explicit cancel by user.
                            }
                            else return false;
                        case ValidityLottery.Preemptive:
                            // Throw BEFORE event
                            Expected.Set(
                                result: resultCancelOrNoop,
                                countChanging: 0,
                                countChanged: 0,
                                countThrow: 1);
                            return true;      // Canceled for cause at call site
                        case ValidityLottery.Responsive:
                            // Throw DURING event
                            Expected.Set(
                                result: resultCancelOrNoop,
                                countChanging: 1,
                                countChanged: 0,
                                countThrow: 1);
                            return true;      // Canceled due to illegal modification in handler.
                        default:
                            throw new NotImplementedException($"Bad case: {Validity}.ToFullKey()");
                    }
                }
            }

            private string? GetExceptions() => "Not Implemented Yet";

            void SetExpectedSuccessResult(object? result)
            {
                if (OCMCall.HasFlag(OCMCall.NotSupported))
                {
                    Debug.Fail($@"ADVISORY - Use enableRange and enableDistinct flags to prevent this.");
                }
                if (MUT.LUT is INotifyCollectionChanging)
                {
                    // [Canonical] success signature
                    Expected.Set(
                        result: result,
                        countChanging: 1,
                        countChanged: 1,
                        countThrow: 0);
                }
                else
                {
                    // [Canonical] success signature
                    Expected.Set(
                        result: result,
                        countChanging: 0,
                        countChanged: 1,
                        countThrow: 0);
                }
            }

            public bool AreEqual()
            {
                if (Expected?.ResultJSON is null)
                {
                    throw new NullReferenceException($"{nameof(Expected)} is null.");
                }
                if (Actual?.ResultJSON is null)
                {
                    throw new NullReferenceException($"{nameof(Actual)} is null.");
                }
                return (Actual.Equals(Expected));
            }

            private object? @void = null;
        }
    }
}
