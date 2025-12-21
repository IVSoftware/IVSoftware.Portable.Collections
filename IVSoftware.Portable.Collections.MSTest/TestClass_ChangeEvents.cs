using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.WinOS.MSTest.Extensions;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Diagnostics;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_ChangeEvents
{
    [TestInitialize]
    public void TestInitialize()
    {
        if (SynchronizationContext.Current is not null)
        {
            Debug.Write($@"ADVISORY - Synchronization Context is not null.");
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
    /// <summary>
    /// Verifies all <see cref="NotifyCollectionChangingEventArgs"/> constructors.
    /// </summary>
    /// <remarks>
    /// - Each subtest exercises one constructor overload or action variant, ensuring
    ///   correct field assignment, index mapping, and ToString output.
    /// - In total, these tests wring out that the entire "Changing" event path is
    ///   ready for integration as a drop-in pre-notification layer complementing
    ///   the BCL's post-change model.
    /// </remarks>
    [TestMethod]
    public void Test_CollectionChangingEventArgs()
    {
        string actual, expected;
        NotifyCollectionChangingEventArgs ePre;
        object[]? newItems, oldItems;

        EventRaiser<string, string> uut = new();
        var builder = new List<string>();

        uut.CollectionChanging += (object? sender, NotifyCollectionChangingEventArgs ePre) =>
        {
            var e = ePre.CopyToChangedEvent();
            builder.Add(ePre.ToString());
            builder.Add(e.ToString(true));

            // PARITY with the BCL: New item is first.
            if (e.NewItems is not null)
            {
                builder.Add($"New Items: {string.Join(", ", ePre.NewItems!.Cast<string>())}");
            }
            if (e.OldItems is not null)
            {
                builder.Add($"Old Items: {string.Join(", ", ePre.OldItems!.Cast<string>())}");
            }
        };

        subtestReset();
        subtestAddSingle();
        subtestRemoveSingle();
        subtestAddSingleWithIndex();
        subtestRemoveSingleWithIndex();
        subtestAddMulti();
        subtestRemoveMulti();
        subtestAddMultiWithIndex();
        subtestRemoveMultiWithIndex();
        subtestReplaceSingle();
        subtestReplaceSingleWithIndex();
        subtestReplaceMulti();
        subtestReplaceMultiWithIndex();
        subtestMoveSingle();
        subtestMoveMulti();
        subtest_ClearItems();


        #region S U B T E S T S
        void subtestReset()
        {
            ePre = new(NotifyCollectionChangingAction.Reset);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Reset, NewItems=null, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Reset, NewItems=null, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting reset action"
            );
        }

        void subtestAddSingle()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Add, "Alpha");
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Alpha"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting add single-item action"
            );
        }

        void subtestRemoveSingle()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Remove, "Alpha");
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Old Items: Alpha"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting remove single-item action"
            );
        }

        void subtestAddSingleWithIndex()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Add, "Bravo", 2);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=1, OldItems=null, NewStartingIndex=2, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=1, OldItems=null, NewStartingIndex=2, OldStartingIndex=-1
New Items: Bravo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting add single-item action with index"
            );
        }

        void subtestRemoveSingleWithIndex()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Remove, "Bravo", 2);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=2
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=2
Old Items: Bravo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting remove single-item action with index"
            );
        }

        void subtestAddMulti()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Add, new[] { "Alpha", "Bravo" });
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=2, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=2, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Alpha, Bravo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting add multi-item action"
            );
        }

        void subtestRemoveMulti()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Remove, new[] { "Alpha", "Bravo" });
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Old Items: Alpha, Bravo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting remove multi-item action"
            );
        }

        void subtestAddMultiWithIndex()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Add, new[] { "Alpha", "Bravo" }, 3);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=2, OldItems=null, NewStartingIndex=3, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=2, OldItems=null, NewStartingIndex=3, OldStartingIndex=-1
New Items: Alpha, Bravo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting add multi-item action with index"
            );
        }

        void subtestReplaceSingle()
        {
            builder.Clear();

            // PARITY with the BCL: New item is first.
            ePre = new (NotifyCollectionChangingAction.Replace, "Bravo", "Charlie");
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Bravo
Old Items: Charlie"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting replace single-item action"
            );
        }

        void subtestRemoveMultiWithIndex()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Remove, new[] { "Alpha", "Bravo" }, 3);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=3
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=3
Old Items: Alpha, Bravo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting remove multi-item action with index"
            );
        }

        void subtestReplaceSingleWithIndex()
        {
            builder.Clear();

            // PARITY with the BCL: New item is first.
            ePre = new(NotifyCollectionChangingAction.Replace, "Bravo", "Charlie", 4);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=4, OldStartingIndex=4
Action=NotifyCollectionChangedAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=4, OldStartingIndex=4
New Items: Bravo
Old Items: Charlie"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting replace single-item action with index"
            );
        }

        void subtestReplaceMulti()
        {
            builder.Clear();

            newItems = new[] { "Alpha", "Bravo" };
            oldItems = new[] { "Charlie", "Delta" };

            ePre = new(NotifyCollectionChangingAction.Replace, newItems, oldItems);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=2, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Replace, NewItems=2, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Alpha, Bravo
Old Items: Charlie, Delta"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting replace multi-item action"
            );
        }

        void subtestReplaceMultiWithIndex()
        {
            builder.Clear();

            newItems = new[] { "Alpha", "Bravo" };
            oldItems = new[] { "Charlie", "Delta" };

            ePre = new(NotifyCollectionChangingAction.Replace, newItems, oldItems, 5);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=2, OldItems=2, NewStartingIndex=5, OldStartingIndex=5
Action=NotifyCollectionChangedAction.Replace, NewItems=2, OldItems=2, NewStartingIndex=5, OldStartingIndex=5
New Items: Alpha, Bravo
Old Items: Charlie, Delta"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting replace multi-item action with index"
            );
        }

        void subtestMoveSingle()
        {
            builder.Clear();

            ePre = new (NotifyCollectionChangingAction.Move, "Echo", 6, 2);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Move, NewItems=1, OldItems=1, NewStartingIndex=6, OldStartingIndex=2
Action=NotifyCollectionChangedAction.Move, NewItems=1, OldItems=1, NewStartingIndex=6, OldStartingIndex=2
New Items: Echo
Old Items: Echo"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting move single-item action"
            );
        }

        void subtestMoveMulti()
        {
            builder.Clear();

            ePre = new (NotifyCollectionChangingAction.Move, new[] { "Echo", "Foxtrot" }, 6, 2);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Move, NewItems=2, OldItems=2, NewStartingIndex=6, OldStartingIndex=2
Action=NotifyCollectionChangedAction.Move, NewItems=2, OldItems=2, NewStartingIndex=6, OldStartingIndex=2
New Items: Echo, Foxtrot
Old Items: Echo, Foxtrot"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting move multi-item action"
            );
        }
        void subtest_ClearItems()
        {
            builder.Clear();

            ePre = new(NotifyCollectionChangingAction.Reset, changedItems: new[] { "Echo", "Foxtrot" });
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Reset, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Reset, NewItems=null, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1"
            ;
        }
        #endregion S U B T E S T S
    }
    #region L o c a l F x 


    /// <summary>
    /// To be clear, this only means that they report
    /// the SAME THING i.e. both right or both wrong!
    /// </summary>
    static void AssertEquivalence(NotifyCollectionChangingEventArgs ePre)
    {
        var e = ePre.CopyToChangedEvent();
        if (ePre.Action == NotifyCollectionChangingAction.Reset)
        {
            Assert.AreEqual(
                "Action=NotifyCollectionChangedAction.Reset, NewItems=null, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1",
                e.ToString(true),
                "Expecting contents to be modified to accomodate BCL Reset.");
        }
        else
        {
            Assert.AreEqual(
                ePre.ToString().Replace("Changing", "Changed"),
                e.ToString(true),
                "Expecting contents to be faithfully transferred to the generated Changed event.");
        }
    }

    #endregion L o c a l F x

    /// <summary>
    /// Uses NotifyCollectionChangingAction keys to test the explicit contract.
    /// </summary>
    /// <remarks>
    /// The faux BCL event doesn't know about Dictionary so it tracks as "native" NotifyCollectionChangingAction
    /// </remarks>
    [TestMethod]
    public void Test_DictionaryChangingEventArgs()
    {
        string actual, expected;
        NotifyCollectionChangingEventArgs ePre;
        DictionaryEntryPreview[]? newItems, oldItems;

        EventRaiser<string, string> uut = new();
        var builder = new List<string>();
        uut.CollectionChanging += (sender, ePre) =>
        {
            var e = ePre.CopyToChangedEvent();
            builder.Add(ePre.ToString());
            builder.Add(e.ToString(true));

            // PARITY with the BCL: New item is first.
            if (e.NewItems is not null)
            {
                builder.Add($"New Items: {string.Join(", ", ePre.NewItems!.Cast<DictionaryEntryPreview>().Select(_=>_.ToString()))}");
            }
            if (e.OldItems is not null)
            {
                builder.Add($"Old Items: {string.Join(", ", ePre.OldItems!.Cast<DictionaryEntryPreview>().Select(_ => _.ToString()))}");
            }
        };

        actual = string.Join(Environment.NewLine, builder);

        subtestReset();
        subtestAddSingle();
        subtestRemoveSingle();
        subtestAddSingleWithIndex();
        subtestRemoveSingleWithIndex();
        subtestAddMulti();
        subtestRemoveMulti();
        subtestAddMultiWithIndex();
        subtestRemoveMultiWithIndex();
        subtestReplaceSingle();
        subtestReplaceSingleWithIndex();
        subtestReplaceMulti();
        subtestReplaceMultiWithIndex();
        subtestMoveSingle();
        subtestMoveMulti();

        #region L O C A L   F X
        static DictionaryEntryPreview localMakePair(string prefix) =>
            new($"{prefix}Key", $"{prefix}Value");

        static DictionaryEntryPreview[] localMakePairs(params string[] prefix) =>
            prefix.Select(_ => localMakePair(_)).ToArray();
        #endregion

        #region S U B T E S T S
        void subtestReset()
        {
            builder.Clear();
            ePre = new (NotifyCollectionChangingAction.Reset);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Reset, NewItems=null, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Reset, NewItems=null, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting reset action");
        }

        void subtestAddSingle()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Add, localMakePair("Alpha"));
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Key=AlphaKey Value=AlphaValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(), 
                actual.NormalizeResult(),
                "Expecting add single-item action");

            actual = JsonConvert.SerializeObject(ePre, Formatting.Indented);
            actual.ToClipboardExpected();
        }

        void subtestRemoveSingle()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Remove, localMakePair("Bravo"));
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Old Items: Key=BravoKey Value=BravoValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(), "Expecting remove single-item action"
			);
        }

        void subtestAddSingleWithIndex()
        {
            builder.Clear();
            ePre = new (NotifyCollectionChangingAction.Add, localMakePair("Charlie"), 2);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=1, OldItems=null, NewStartingIndex=2, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=1, OldItems=null, NewStartingIndex=2, OldStartingIndex=-1
New Items: Key=CharlieKey Value=CharlieValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestRemoveSingleWithIndex()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Remove, localMakePair("Delta"), 2);
            AssertEquivalence(ePre);

            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=2
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=2
Old Items: Key=DeltaKey Value=DeltaValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestAddMulti()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Add, localMakePairs("Echo", "Foxtrot"));
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=2, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=2, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Key=EchoKey Value=EchoValue, Key=FoxtrotKey Value=FoxtrotValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestRemoveMulti()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Remove, localMakePairs("Echo", "Foxtrot"));
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Old Items: Key=EchoKey Value=EchoValue, Key=FoxtrotKey Value=FoxtrotValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestAddMultiWithIndex()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Add, localMakePairs("Echo", "Foxtrot", "Golf"), 3);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=3, OldItems=null, NewStartingIndex=3, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=3, OldItems=null, NewStartingIndex=3, OldStartingIndex=-1
New Items: Key=EchoKey Value=EchoValue, Key=FoxtrotKey Value=FoxtrotValue, Key=GolfKey Value=GolfValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestRemoveMultiWithIndex()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Remove, localMakePairs("Echo", "Foxtrot", "Golf", "Hotel"), 3);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=4, NewStartingIndex=-1, OldStartingIndex=3
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=4, NewStartingIndex=-1, OldStartingIndex=3
Old Items: Key=EchoKey Value=EchoValue, Key=FoxtrotKey Value=FoxtrotValue, Key=GolfKey Value=GolfValue, Key=HotelKey Value=HotelValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestReplaceSingle()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Replace, localMakePair("India"), localMakePair("Juliet"));
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Key=IndiaKey Value=IndiaValue
Old Items: Key=JulietKey Value=JulietValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestReplaceSingleWithIndex()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Replace, localMakePair("Kilo"), localMakePair("Lima"), 4);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=4, OldStartingIndex=4
Action=NotifyCollectionChangedAction.Replace, NewItems=1, OldItems=1, NewStartingIndex=4, OldStartingIndex=4
New Items: Key=KiloKey Value=KiloValue
Old Items: Key=LimaKey Value=LimaValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestReplaceMulti()
        {
            builder.Clear();
            newItems = localMakePairs("Foxtrot", "Golf");
            oldItems = localMakePairs("Mike", "November");
            ePre = new(NotifyCollectionChangingAction.Replace, newItems, oldItems);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=2, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Replace, NewItems=2, OldItems=2, NewStartingIndex=-1, OldStartingIndex=-1
New Items: Key=FoxtrotKey Value=FoxtrotValue, Key=GolfKey Value=GolfValue
Old Items: Key=MikeKey Value=MikeValue, Key=NovemberKey Value=NovemberValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestReplaceMultiWithIndex()
        {
            builder.Clear();
            newItems = localMakePairs("Echo", "Foxtrot", "Golf");
            oldItems = localMakePairs("Mike", "November", "Oscar");
            ePre = new(NotifyCollectionChangingAction.Replace, newItems, oldItems, 5);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Replace, NewItems=3, OldItems=3, NewStartingIndex=5, OldStartingIndex=5
Action=NotifyCollectionChangedAction.Replace, NewItems=3, OldItems=3, NewStartingIndex=5, OldStartingIndex=5
New Items: Key=EchoKey Value=EchoValue, Key=FoxtrotKey Value=FoxtrotValue, Key=GolfKey Value=GolfValue
Old Items: Key=MikeKey Value=MikeValue, Key=NovemberKey Value=NovemberValue, Key=OscarKey Value=OscarValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestMoveSingle()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Move, localMakePair("Quebec"), 6, 2);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Move, NewItems=1, OldItems=1, NewStartingIndex=6, OldStartingIndex=2
Action=NotifyCollectionChangedAction.Move, NewItems=1, OldItems=1, NewStartingIndex=6, OldStartingIndex=2
New Items: Key=QuebecKey Value=QuebecValue
Old Items: Key=QuebecKey Value=QuebecValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestMoveMulti()
        {
            builder.Clear();
            ePre = new(NotifyCollectionChangingAction.Move, localMakePairs("Quebec", "Romeo"), 6, 2);
            AssertEquivalence(ePre);
            uut.RaiseCollectionChanging(ePre);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Move, NewItems=2, OldItems=2, NewStartingIndex=6, OldStartingIndex=2
Action=NotifyCollectionChangedAction.Move, NewItems=2, OldItems=2, NewStartingIndex=6, OldStartingIndex=2
New Items: Key=QuebecKey Value=QuebecValue, Key=RomeoKey Value=RomeoValue
Old Items: Key=QuebecKey Value=QuebecValue, Key=RomeoKey Value=RomeoValue"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }
        #endregion S U B T E S T S
    }

    class EventRaiser
    {
        public void RaiseCollectionChanging(NotifyCollectionChangingEventArgs e, bool coerce = false) => OnCollectionChanging(e, coerce);
        protected virtual void OnCollectionChanging(NotifyCollectionChangingEventArgs e, bool coerce)
        {
            CollectionChanging?.Invoke(this, e);
        }
        public event NotifyCollectionChangingEventHandler? CollectionChanging;


        public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e);

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
    class EventRaiser<TKey, TValue> : EventRaiser where TKey : notnull { }
}
