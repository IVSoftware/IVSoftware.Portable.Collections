using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.MSTest.Mutator;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.WinOS.MSTest.Extensions;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_PHLANX
{

    [TestInitialize]
    public void TestInitialize()
    {
        if(SynchronizationContext.Current is not null)
        {
            Debug.Write($@"ADVISORY - Synchronization Context is not null.");
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
#if false
    /// <summary>
    /// Phlanx is 100% exercisable through the IList interface.
    /// </summary>
    /// <remarks>
    /// The purpos of this test is to touch on the main interface
    /// entry points in a friendly manner.
    /// </remarks>
    [TestMethod]
    public void Test_TrialRunWithIList()
    {
        const int SEED = 1;
        string actual, expected;
        bool isCancelRequested = false;

        List<string>
            builder = new(),
            throws = new();
        IList ilist = 
            new Phlanx<int>() { OptimizationMode = ListOptimizationMode.UseCacheForContains };
        int 
            countChanging = 0,
            countChanged = 0;
        var eBatch =
            new NotifyPreviewCollectionChangingEventArgs(
                action: NotifyPreviewCollectionChangingAction.Batch
            );
        var rando = 
            new Random(SEED);

        #region L o c a l F x	
        void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
        {
            countChanging++;
            e.Cancel = isCancelRequested;
        }
        void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            countChanged++;
        }

        void localOnBeginThrowOrAdvise(object? sender, Throw e)
        {
            throws.Add(e.FormattedMessage);
            e.Handled = true;
        }
        void localClear(bool clearList, int seed = SEED)
        {
            rando = new Random(seed);
            if (clearList) ilist.Clear();
            countChanging = countChanged = 0;
            eBatch.NewItems!.Clear();
            throws.Clear();
            builder.Clear();
        }

        void localAddTestRange(int n = 10, int min = 0, int max = 100, bool inclusive = true)
        {
            var countThrowsB4 = throws.Count;
            var items = 
                Enumerable.Range(1, n)
                .Select(_ => rando.Next(min, inclusive ? max + 1 : max ))
                .ToList();

            ((Phlanx<int>)ilist).AddRange(items);

            if (isCancelRequested)
            {
                Assert.AreEqual(
                    countThrowsB4 + 1,
                    throws.Count,
                    $"Expecting an operation canceled exception.");
            }
            else
            {
                Assert.AreEqual(
                    countThrowsB4,
                    throws.Count,
                    $"Expecting an error-free operation.");
            }
        }
        using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                ((INotifyCollectionChanging)ilist).CollectionChanging += localOnCollectionChanging;
                ((INotifyCollectionChanged)ilist).CollectionChanged += localOnCollectionChanged;
                Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
            },
            onDispose: (sender, e) =>
            {
                ((INotifyCollectionChanging)ilist).CollectionChanging -= localOnCollectionChanging;
                ((INotifyCollectionChanged)ilist).CollectionChanged -= localOnCollectionChanged;
            });
        #endregion L o c a l F x

        Assert.AreEqual(ListOptimizationMode.UseCacheForContains, ((IPhlanx)ilist).OptimizationMode );

        // SAVE - If we need to temporarily toggle this off...
        // ((IPhlanx)ilist).OptimizationMode = ListOptimizationMode.Normal;

        subtest_Add_ValidItem();
        subtest_Add_InvalidType();
        subtest_IndexerGet_ValidIndex();
        subtest_IndexerGet_InvalidIndex();
        subtest_IndexerSet_ValidItem();
        subtest_IndexerSet_InvalidType();
        subtest_IndexerSet_InvalidIndex();
        subtest_Contains_ExistingItem();
        subtest_Contains_NonExistingItem();
        subtest_IndexOf_ExistingItem();
        subtest_IndexOf_NonExistingItem();
        subtest_IndexOf_InvalidType();
        subtest_Insert_ValidItem();
        subtest_Remove_ValidItem();
        subtest_RemoveAt_ValidIndex();
        subtest_CopyTo_CompatibleArray();
        subtest_Clear_EmptiesCollection();
        subtest_IsFixedSize_IsFalse();
        subtest_IsSynchronized_IsFalse();
        subtest_SyncRoot_ReturnsSelf();
        subtest_Enumerate_EmptyList();
        subtest_Enumerate_AfterAdds();
        subtest_IListContract_AllowsOnlyT();


        #region S U B T E S T S

        void subtest_Add_ValidItem()
        {
            localClear(true);

            var addedIndex = ilist.Add(100);

            Assert.AreEqual(0, addedIndex);
            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);
            Assert.AreEqual(0, throws.Count);

            actual = JsonConvert.SerializeObject(ilist);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[100]"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting One item in list"
            );

            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }))
            {
                ilist.Add(200);
            }
            Assert.AreEqual(2, countChanging);
            Assert.AreEqual(1, countChanged);
            Assert.AreEqual(1, throws.Count, $"Expecting operation canceled");

            actual = string.Join(Environment.NewLine, throws);
            actual.ToClipboardExpected();

            { }
            expected = @" 
ApplyChanges | OperationCanceledException";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting operation canceled."
            );
        }

        void subtest_IndexerGet_ValidIndex()
        {
            localClear(true);
            localAddTestRange();

            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);
            Assert.AreEqual(10, ilist.Count);
            Assert.AreEqual(0, throws.Count);


            actual = JsonConvert.SerializeObject(ilist);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[25,11,47,77,66,43,35,95,10,64]"
            ;

            // Do not clear
            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting json serialization to match."
            );

            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }))
            {
                localAddTestRange();
            }

            Assert.AreEqual(2, countChanging);
            Assert.AreEqual(1, countChanged);
            Assert.AreEqual(10, ilist.Count);
            Assert.AreEqual(1, throws.Count, $"Expecting op canceled");
            throws.Clear();

            localAddTestRange();

            Assert.AreEqual(3, countChanging);
            Assert.AreEqual(2, countChanged);
            Assert.AreEqual(20, ilist.Count);
            Assert.AreEqual(0, throws.Count);


            actual = JsonConvert.SerializeObject(ilist);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[25,11,47,77,66,43,35,95,10,64,95,9,16,38,80,17,80,31,83,89]"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 10 additional items appended."
            );
        }

        void subtest_IndexerSet_ValidItem()
        {
            localClear(true);

            ilist[0] = 10;
            countChanging = countChanged = 0;

            ilist[0] = 20;
            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);
            Assert.AreEqual(1, ilist.Count);

            Assert.AreEqual(20, ilist[0], $"Expecting loopback.");
        }

        void subtest_Contains_ExistingItem()
        {
            localClear(true);

            ilist.Add(40);
            Assert.IsTrue(ilist.Contains(40));
        }

        void subtest_Contains_NonExistingItem()
        {
            localClear(true);

            ilist.Add(50);
            Assert.IsFalse(ilist.Contains(999));
        }

        void subtest_IndexOf_ExistingItem()
        {
            localClear(true);

            ilist.Add(77);
            ilist.Add(88);

            Assert.AreEqual(0, ilist.IndexOf(77));
            Assert.AreEqual(1, ilist.IndexOf(88));
        }

        void subtest_IndexOf_NonExistingItem()
        {
            localClear(true);

            ilist.Add(1);
            ilist.Add(2);

            Assert.AreEqual(-1, ilist.IndexOf(999));
        }

        void subtest_Insert_ValidItem()
        {
            localClear(true);

            ilist.Add(1);
            ilist.Add(3);
            countChanging = countChanged = 0;

            ilist.Insert(1, 2);

            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);

            actual = JsonConvert.SerializeObject(ilist);

            actual.ToClipboardExpected();
            { }
            expected = @" 
[1,2,3]"
            ;
            Assert.AreEqual(expected.NormalizeResult(), actual.NormalizeResult());
        }

        void subtest_Remove_ValidItem()
        {
            localClear(true);

            ilist.Add(10);
            ilist.Add(20);
            ilist.Add(30);
            countChanging = countChanged = 0;

            ilist.Remove(20);

            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);

            actual = JsonConvert.SerializeObject(ilist);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[10,30]"
            ;

            Assert.AreEqual(expected.NormalizeResult(), actual.NormalizeResult());
        }

        void subtest_RemoveAt_ValidIndex()
        {
            localClear(true);

            ilist.Add(1);
            ilist.Add(2);
            ilist.Add(3);
            countChanging = countChanged = 0;

            ilist.RemoveAt(1);

            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);

            actual = JsonConvert.SerializeObject(ilist, Formatting.Indented);
            { }
            expected = @" 
[
  1,
  3
]";
            Assert.AreEqual(expected.NormalizeResult(), actual.NormalizeResult());
        }

        void subtest_CopyTo_CompatibleArray()
        {
            localClear(true);

            ilist.Add(9);
            ilist.Add(8);
            ilist.Add(7);

            var arr = new int[3];
            ilist.CopyTo(arr, 0);

            Assert.AreEqual(9, arr[0]);
            Assert.AreEqual(8, arr[1]);
            Assert.AreEqual(7, arr[2]);
        }

        void subtest_Clear_EmptiesCollection()
        {
            localClear(true);

            ilist.Add(1);
            ilist.Add(2);
            ilist.Add(3);
            countChanging = countChanged = 0;

            ilist.Clear();

            Assert.AreEqual(1, countChanging);
            Assert.AreEqual(1, countChanged);
            Assert.AreEqual(0, ilist.Count);
        }

        void subtest_IsFixedSize_IsFalse()
        {
            localClear(true);
            Assert.IsFalse(ilist.IsFixedSize);
        }

        void subtest_IsSynchronized_IsFalse()
        {
            localClear(true);
            Assert.IsFalse(((ICollection)ilist).IsSynchronized);
        }

        void subtest_SyncRoot_ReturnsSelf()
        {
            localClear(true);
            Assert.AreSame(ilist, ((ICollection)ilist).SyncRoot);
        }

        void subtest_Enumerate_EmptyList()
        {
            localClear(true);

            foreach (var _ in ilist)
            {
                Assert.Fail("Should not enumerate any items.");
            }
        }

        void subtest_Enumerate_AfterAdds()
        {
            localClear(true);

            ilist.Add(1);
            ilist.Add(2);
            ilist.Add(3);

            int sum = 0;
            foreach (var v in ilist)
            {
                sum += (int)v;
            }

            Assert.AreEqual(6, sum);
        }

        void subtest_IListContract_AllowsOnlyT()
        {
            localClear(true);

            Assert.IsFalse(ilist.Contains("abc"));
            Assert.AreEqual(-1, ilist.IndexOf("abc"));
        }

        void subtest_Add_InvalidType()
        {
            localClear(true);

            ilist.Add(Math.PI);

            actual = string.Join(Environment.NewLine, throws);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Add | Invalid cast in Add(object?).";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting cast exception."
            );
        }

        void subtest_IndexerGet_InvalidIndex()
        {
            localClear(true);

            _ = ilist[10];

            Assert.AreEqual(1, throws.Count);

            actual = string.Join(Environment.NewLine, throws);
            actual.ToClipboardAssert("Expecting out of range.");
            { }
            expected = @" 
Item | IndexOutOfRangeException";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting out of range."
            );
        }

        void subtest_IndexerSet_InvalidType()
        {
            int localTestCount = 0;
            localTest();

            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }))
            {
                // The cancel request is N.A. because execution never gets that far.
                localTest();
            }

            void localTest()
            {
                localClear(true);

                ilist[0] = Math.PI;

                Assert.AreEqual(0, countChanging, $"Expecting preemptive exception.");
                Assert.AreEqual(0, countChanged, $"Expecting preemptive exception.");
                Assert.AreEqual(0, ilist.Count);
                Assert.AreEqual(1, throws.Count);

                actual = string.Join(Environment.NewLine, throws);
                actual.ToClipboardExpected();
                { }
                expected = @" 
System.Collections.IList.Item | Invalid cast in non-generic indexer set.";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
                localTestCount++;
            }

            Assert.AreEqual(2, localTestCount, $"Expecting localTest ran twice.");
        }

        void subtest_IndexerSet_InvalidIndex()
        {
            int localTestCount = 0;
            localTest();

            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }))
            {
                // The cancel request is N.A. because execution never gets that far.
                localTest();
            }

            void localTest()
            {
                localClear(true);

                ilist[100] = 0;

                Assert.AreEqual(0, countChanging, $"Expecting preemptive exception.");
                Assert.AreEqual(0, countChanged, $"Expecting preemptive exception.");
                Assert.AreEqual(0, ilist.Count);
                Assert.AreEqual(1, throws.Count);

                actual = string.Join(Environment.NewLine, throws);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Item | IndexOutOfRangeException"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
                localTestCount++;
            }
            Assert.AreEqual(2, localTestCount, $"Expecting localTest ran twice.");
        }

        void subtest_IndexOf_InvalidType()
        {
            localClear(true);
        }
        #endregion S U B T E S T S
    }
#endif

    [TestMethod]
    public void Test_BeforeItemCreateEventArgs()
    {
        string actual, expected;

        RandomOrNull rando = new(50);
        var builder =
            Enumerable
            .Range(1, 100)
            .Select(_ => new BeforeItemCreateEventArgs(typeof(string), rando).Item?.ToString() ?? "-------- null value generated")
            .ToArray();

        actual = string.Join(Environment.NewLine, builder);
        actual.ToClipboardExpected();
        { }
        expected = @" 
Whiskey
Mike
Sierra
Foxtrot
Uniform
Golf
India
-------- null value generated
November
-------- null value generated
Echo
Sierra
Delta
-------- null value generated
November
Alpha
Mike
Zulu
-------- null value generated
-------- null value generated
Golf
Whiskey
Zulu
Alpha
Romeo
Lima
Charlie
Lima
Mike
Papa
-------- null value generated
Romeo
Xray
Romeo
Juliett
Juliett
Quebec
Quebec
-------- null value generated
Kilo
Golf
Charlie
Yankee
-------- null value generated
Lima
Lima
Xray
Juliett
Xray
Papa
Sierra
Yankee
Golf
Victor
Juliett
-------- null value generated
Golf
Alpha
Victor
Foxtrot
Golf
Yankee
Romeo
Foxtrot
-------- null value generated
Tango
Bravo
Foxtrot
Quebec
Charlie
India
Quebec
Echo
Victor
Bravo
November
-------- null value generated
Charlie
-------- null value generated
Tango
Juliett
Bravo
Victor
Lima
Charlie
Romeo
Uniform
Whiskey
Kilo
November
Golf
Uniform
India
Romeo
Quebec
November
Delta
Kilo
Romeo
Sierra"
        ;

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting NATO strings with sporadic null values."
        );
    }

    [TestMethod]
    public void Test_OCM_OPCDistinctNoCacheOrRange()
    {
        int? SEED = null;

        using var mut = new ObservableCollectionMutator(
            lut: new ObservablePreviewCollection<int?>(),
            enableRange: false,
            control: new ObservableCollection<int?>(),
            seed: SEED
        );
        Assert.IsTrue(mut.LUTOPC?.OptimizationMode == Lists.ListOptimizationMode.Normal);

        // 'A' Loop
        string actual, expected = string.Empty;


        int STOP_ON = 1000;

        for (mut.LoopIndex = 0; mut.LoopIndex < STOP_ON + 1; mut.LoopIndex++)
        {
            Assert.AreEqual(0, mut.CountDistinctifierContains, "Expecting non-cached list");

            Debug.WriteLine($"251121.A Loop={mut.LoopIndex,-3} SEED={mut.SEED}");
            Console.WriteLine($"251121.A Loop={mut.LoopIndex,-3} SEED={mut.SEED}");
            bool stopOn = mut.LoopIndex == STOP_ON;

            var stim = mut.RunMutation(stopOn: stopOn);
            Debug.WriteLine($"251121.A Loop={mut.LoopIndex,-3} {stim.OCMCall.ToFullKey()}");

            actual = JsonConvert.SerializeObject(stim, Formatting.Indented);
            actual.ToClipboardExpected();

            if (stim.AreEqual())
            {
                // The counts will VARY depending on whether incChanging is implemented. But
                // the CONTROL result should always be the same in terms of what it contains.
                if (stopOn)
                {   /* G T K */
                }
                else
                {   /* G T K */
                }
            }
            else
            {
                // Fail if not a known debugging inprog index
                if (stopOn)
                {   /* G T K */
                }
                else
                {
                    Assert.Fail();
                }
            }
            expected = @" 
{
  ""LoopIndex"": 74,
  ""CountDistinctifierContains"": 0,
  ""OCMCall"": ""AddDistinct"",
  ""IsValidNOOP"": false,
  ""InitialState"": ""[29,null,null,155,22,107,16,28,47,105,65,109,186,135,null,42,127,224,null,193,32,12,190,117,166,238,237,73,125,36,69,188,111,205]"",
  ""Validity"": ""Preemptive"",
  ""ErrorInjectFlag"": 0,
  ""IsCancelRequested"": false,
  ""ExpectedException"": null,
  ""Valid"": {
    ""Action"": 0,
    ""NewStartingIndex"": -1,
    ""OldStartingIndex"": -1,
    ""NewItems"": [
      218
    ],
    ""OldItems"": null
  },
  ""Invalid"": {
    ""Action"": 0,
    ""NewStartingIndex"": -1,
    ""OldStartingIndex"": -1,
    ""NewItems"": [
      ""75fe8101-47cb-4bad-a72c-7c468254d211""
    ],
    ""OldItems"": null
  },
  ""Expected"": {
    ""Phase"": ""Expected"",
    ""ResultJSON"": ""[29,null,null,155,22,107,16,28,47,105,65,109,186,135,null,42,127,224,null,193,32,12,190,117,166,238,237,73,125,36,69,188,111,205]"",
    ""Result"": true,
    ""CountChanging"": 0,
    ""CountChanged"": 0,
    ""CountThrow"": 1
  },
  ""Actual"": {
    ""Phase"": ""Actual"",
    ""ResultJSON"": ""[29,null,null,155,22,107,16,28,47,105,65,109,186,135,null,42,127,224,null,193,32,12,190,117,166,238,237,73,125,36,69,188,111,205]"",
    ""Result"": false,
    ""CountChanging"": 0,
    ""CountChanged"": 0,
    ""CountThrow"": 1
  }
}"
            ;

        }
    }

    [TestMethod]
    public void Test_OCM_OPCDistinctCacheNoRange()
    {
        int? SEED = null;

        using var mut = new ObservableCollectionMutator(
            lut: new ObservablePreviewCollection<int?>
            {
                OptimizationMode = Lists.ListOptimizationMode.UseCacheForContains
            },
            enableRange: false,
            control: new ObservableCollection<int?>(),
            seed: SEED
        );

        // 'B' Loop
        string actual, expected = string.Empty;

        int STOP_ON = 1000;

        for (mut.LoopIndex = 0; mut.LoopIndex < STOP_ON + 1; mut.LoopIndex++)
        {
            Debug.WriteLine($"251121.A Loop={mut.LoopIndex,-3} SEED={mut.SEED}");
            Console.WriteLine($"251121.A Loop={mut.LoopIndex,-3} SEED={mut.SEED}");
            bool stopOn = mut.LoopIndex == STOP_ON;

            var stim = mut.RunMutation(stopOn: stopOn);
            Debug.WriteLine($"251121.A Loop={mut.LoopIndex,-3} {stim.OCMCall.ToFullKey()}");

            stim.CountDistinctifierContains = mut.CountDistinctifierContains;

            actual = JsonConvert.SerializeObject(stim, Formatting.Indented);
            actual.ToClipboardExpected();

            if (stim.AreEqual())
            {
                // The counts will VARY depending on whether incChanging is implemented. But
                // the CONTROL result should always be the same in terms of what it contains.
                if (stopOn)
                {   /* G T K */
                }
                else
                {   /* G T K */
                }
            }
            else
            {
                // Fail if not a known debugging inprog index
                if (stopOn)
                {   /* G T K */
                }
                else
                {
                    Assert.Fail();
                }
            }
        }
        expected = @" 
{
  ""LoopIndex"": 1000,
  ""CountDistinctifierContains"": 65,
  ""OCMCall"": ""RemoveAt"",
  ""IsValidNOOP"": false,
  ""InitialState"": ""[116,129,128,24,47,134,68,114,129,null,197,63,null,95,113,70,192,127,248,42,121,7,92,null,112,101,115,23,144,null,null,213,221,116]"",
  ""Validity"": ""Valid"",
  ""ErrorInjectFlag"": 0,
  ""IsCancelRequested"": false,
  ""IsBatchRequested"": false,
  ""ExpectedException"": null,
  ""Valid"": {
    ""Action"": 1,
    ""NewStartingIndex"": -1,
    ""OldStartingIndex"": 32,
    ""NewItems"": null,
    ""OldItems"": [
      62
    ]
  },
  ""Invalid"": {
    ""Action"": 1,
    ""NewStartingIndex"": -1,
    ""OldStartingIndex"": 185,
    ""NewItems"": null,
    ""OldItems"": [
      ""3d7893cb-2008-41cb-b24b-524e81facfd8""
    ]
  },
  ""Expected"": {
    ""Phase"": ""Expected"",
    ""ResultJSON"": ""[116,129,128,24,47,134,68,114,129,null,197,63,null,95,113,70,192,127,248,42,121,7,92,null,112,101,115,23,144,null,null,213,116]"",
    ""Result"": null,
    ""CountChanging"": 1,
    ""CountChanged"": 1,
    ""CountThrow"": 0
  },
  ""Actual"": {
    ""Phase"": ""Actual"",
    ""ResultJSON"": ""[116,129,128,24,47,134,68,114,129,null,197,63,null,95,113,70,192,127,248,42,121,7,92,null,112,101,115,23,144,null,null,213,116]"",
    ""Result"": null,
    ""CountChanging"": 1,
    ""CountChanged"": 1,
    ""CountThrow"": 0
  }
}"
            ;
    }

    [TestMethod]
    public void Test_OCM_OPCDistinctRangeCache()
    {
        int? SEED = null;

        using var mut = new ObservableCollectionMutator(
            lut: new ObservablePreviewCollection<int?>
            {
                OptimizationMode = Lists.ListOptimizationMode.UseCacheForContains
            },
            enableRange: true,
            control: new ObservableCollection<int?>(),
            seed: SEED
        );

        // 'C' Loop
        string actual, expected = string.Empty;

        int STOP_ON = 1000;

        for (mut.LoopIndex = 0; mut.LoopIndex < STOP_ON + 1; mut.LoopIndex++)
        {
            Debug.WriteLine($"251121.A Loop={mut.LoopIndex,-3} SEED={mut.SEED}");
            Console.WriteLine($"251121.A Loop={mut.LoopIndex,-3} SEED={mut.SEED}");
            bool stopOn = mut.LoopIndex == STOP_ON;

            var stim = mut.RunMutation(stopOn: stopOn);
            Debug.WriteLine($"251121.A Loop={mut.LoopIndex,-3} {stim.OCMCall.ToFullKey()}");

            stim.CountDistinctifierContains = mut.CountDistinctifierContains;

            actual = JsonConvert.SerializeObject(stim, Formatting.Indented);
            actual.ToClipboardExpected();

            if (stim.AreEqual())
            {
                // The counts will VARY depending on whether incChanging is implemented. But
                // the CONTROL result should always be the same in terms of what it contains.
                if (stopOn)
                {   /* G T K */
                }
                else
                {   /* G T K */
                }
            }
            else
            {
                // Fail if not a known debugging inprog index
                if (stopOn)
                {   /* G T K */
                }
                else
                {
                    Assert.Fail($"Stim is not equal on Loop {stim.LoopIndex}.");
                }
            }
        }
        expected = @" 
{
  ""LoopIndex"": 1,
  ""SEED"": 10,
  ""CountDistinctifierContains"": 0,
  ""OCMCall"": ""Add"",
  ""IsValidNOOP"": false,
  ""InitialState"": ""[191,null,34,224,151,152,139,145,60,78,92,43,193,null,132,55,102,135,197,56,14,40,190,38,30,61,137]"",
  ""Validity"": ""Valid"",
  ""ErrorInjectFlag"": 0,
  ""IsCancelRequested"": false,
  ""ExpectedException"": null,
  ""Valid"": {
    ""Action"": 0,
    ""NewStartingIndex"": -1,
    ""OldStartingIndex"": -1,
    ""NewItems"": [
      14
    ],
    ""OldItems"": null
  },
  ""Invalid"": {
    ""Action"": 0,
    ""NewStartingIndex"": -1,
    ""OldStartingIndex"": -1,
    ""NewItems"": [
      ""edf8d335-1a03-4d81-aeb3-00cc56b9a93b""
    ],
    ""OldItems"": null
  },
  ""Expected"": {
    ""Phase"": ""Expected"",
    ""ResultJSON"": ""[191,null,34,224,151,152,139,145,60,78,92,43,193,null,132,55,102,135,197,56,14,40,190,38,30,61,137,14]"",
    ""Result"": 27,
    ""CountChanging"": 1,
    ""CountChanged"": 1,
    ""CountThrow"": 0
  },
  ""Actual"": {
    ""Phase"": ""Actual"",
    ""ResultJSON"": ""[191,null,34,224,151,152,139,145,60,78,92,43,193,null,132,55,102,135,197,56,14,40,190,38,30,61,137,14]"",
    ""Result"": 1,
    ""CountChanging"": 1,
    ""CountChanged"": 1,
    ""CountThrow"": 0
  }
}"
        ;
    }
}