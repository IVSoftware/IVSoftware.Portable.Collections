using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.MSTest.TestTargets;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.WinOS.MSTest.Extensions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_OpinionatedDictionaries
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

    [TestCleanup]
    public void TestCleanup() { }

    static int _clearCountDebug = 0;

    [TestMethod]
    public void Test_ObservableDictionary()
    {
        string actual, expected;
        List<string> builder = new();
        int eventCount = 0, coercedValueCount = 0;

        // These control how the LOCAL HANDLER responds.
        bool 
            isCancelRequested = false,
            isCoerceRequested = false;

        IObservableDictionary dut = new ObservableDictionary<string, string>();

        #region L o c a l F x 
        using var _ = dut.WithOnDispose(
            onInit: (sender, e) =>
            {
                dut.CollectionChanging += localOnCollectionChanging;
                dut.CollectionChanged += localOnCollectionChanged;
                Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
            },
            onDispose: (sender, e) =>
            {
                dut.CollectionChanging -= localOnCollectionChanging;
                dut.CollectionChanged -= localOnCollectionChanged;
                Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
            });

        void localOnBeginThrowOrAdvise(object? sender, Throw e)
        {
            builder.Add($"{e.ToString(ThrowToStringFormat.MSTest)}");
            e.Handled = true;
        }
        void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
        {
            builder.Add($"Event = {++eventCount:D3}");

            builder.Add($"C O L L E C T I O N    C H A N G I N G    E V E N T");
            builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));

            if (isCancelRequested)
            {
                builder.Add("CancelRequested");
                e.Cancel = true;
            }
            if (isCoerceRequested)
            {
                builder.Add("CoerceRequested");
                switch (e.NewItems.GetStatusAsList())
                {
                    case StatusAsList.Single:
                        // [Careful]
                        // Do 'not' check IsCoerced because "this is that phone call".
                        if (e.GetNewItemSingle() is DictionaryEntryPreview entry)
                        {
                            entry.Value = $"CoercedValue{++coercedValueCount}";
                        }
                        break;
                    case StatusAsList.Null:
                    case StatusAsList.Empty:
                    case StatusAsList.Multi:
                    default:
                        throw new NotImplementedException("Unexpected");
                }
            }
            if (isCoerceRequested || isCancelRequested)
            {
                builder.Add($"A F T E R    C O E R C E    O R    C A N C E L");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                builder.Add(string.Empty);
            }
        }
        void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            eventCount++;

            builder.Add($"Event = {eventCount:D3}");
            builder.Add($"C O L L E C T I O N    C H A N G E D    E V E N T");
            builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
        }
        void localClearAll(bool includeDict = false)
        {
            // 1. Cancel the bools. Otherwise, Dict might not clear.
            isCancelRequested = false;
            isCoerceRequested = false;
            // 2. Empty out the dict.
            if(includeDict)
            {
                dut.Clear();
                Assert.AreEqual(
                    0, 
                    dut.Count, 
                    $"[{_clearCountDebug}] Expecting dut count is 0 after clear.");
            }
            // 3. Reset the loc and event count
            builder.Clear();
            eventCount = 0;
        }
#endregion L o c a l F x

        subtestAddAndAddCoerce();
        subtestRemoveAndRemoveCancel();
        subtestClearAndClearCancel();
        subtestReplaceAndReplaceCancel();
        subtestDHostEphemeral();

        #region S U B T E S T S
        void subtestAddAndAddCoerce()
        {
            localClearAll(includeDict: true);
            dut["Item1"] = "Value1";
            Assert.AreEqual(1, dut.Count);
            Assert.AreEqual(2, eventCount, $"Expecting 2 events");

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item1"",
      ""Value"": ""Value1""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item1"",
      ""Value"": ""Value1""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );

            // C o e r c e
            localClearAll(includeDict: true);


            using (this.WithOnDispose(
                    onInit: (sender, e) =>
                    {
                        isCoerceRequested = true;
                    },
                    onDispose: (sender, e) =>
                    {
                        isCoerceRequested = false;
                    }
                ))
            {
                dut["Item2"] = "CoerceMe!";
                Assert.AreEqual(1, dut.Count);
                Assert.AreEqual(2, eventCount, $"Expecting 2 events");
            }

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item2"",
      ""Value"": ""CoerceMe!""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
CoerceRequested
A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item2"",
      ""Value"": ""CoercedValue1""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item2"",
      ""Value"": ""CoercedValue1""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );

            localClearAll(includeDict: true);
            dut.Add("Item3", "Value3");
            Assert.AreEqual(1, dut.Count);
            Assert.AreEqual(2, eventCount, $"Expecting 2 events");

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item3"",
      ""Value"": ""Value3""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""Item3"",
      ""Value"": ""Value3""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
            localClearAll();
            dut.Add("Item3", "Value3");

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
ThrowHard
Type: ArgumentException
Id: Add
An element with the same key ('Item3') already exists in the dictionary.";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestRemoveAndRemoveCancel()
        {
            localClearAll(includeDict: true);

            // Put the event in (not what we're testing)
            dut["RemoveKey"] = "RemoveValue";
            Assert.AreEqual(1, dut.Count); // But here it is.
            Assert.AreEqual(2, eventCount, $"Expecting 2 events");

            localClearAll();
            dut.Remove("RemoveKey");
            Assert.AreEqual(0, dut.Count);
            Assert.AreEqual(2, eventCount, $"Expecting 2 events");

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    {
      ""Key"": ""RemoveKey"",
      ""Value"": ""RemoveValue""
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    {
      ""Key"": ""RemoveKey"",
      ""Value"": ""RemoveValue""
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );

            localClearAll();
            // Put the event in (not what we're testing)
            dut["RemoveKey"] = "RemoveValue";
            Assert.AreEqual(1, dut.Count); // But here it is.
            Assert.AreEqual(2, eventCount, $"Expecting 2 events");

            // Clear events but not the DUT
            localClearAll();
            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }
                ))
            {
                dut.Remove("RemoveKey");

                Assert.AreEqual(
                    1, eventCount,
                    $"DIFFERENT! 1 event only because remove was cancelled.");

                Assert.AreEqual(
                    1,
                    dut.Count,
                    $"Expecting item removal successfully canceled.");

                // You can see it in the readout below.
                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    {
      ""Key"": ""RemoveKey"",
      ""Value"": ""RemoveValue""
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
CancelRequested
A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    {
      ""Key"": ""RemoveKey"",
      ""Value"": ""RemoveValue""
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": true
}

ThrowSoft
Type: OperationCanceledException
Id: OnCollectionChanging
OperationCanceledException
ThrowSoft
Type: OperationCanceledException
Id: Remove
OperationCanceledException"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder contain cancel info."
                );
            }
        }

        void subtestClearAndClearCancel()
        {
            // 1. Populate dictionary with 10 whimsical entries.
            localClearAll(includeDict: true);
            Assert.AreEqual(0, dut.Count);
            dut.AddRange(
            [
                new("flarn","mib"),
                new("tovel","crin"),
                new("sproot","dax"),
                new("blen","quar"),
                new("nith","glom"),
                new("prax","wend"),
                new("zoril","vemm"),
                new("trindle","fesk"),
                new("marn","cloop"),
                new("velth","snor")
            ]);


            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""flarn"",
      ""Value"": ""mib""
    },
    {
      ""Key"": ""tovel"",
      ""Value"": ""crin""
    },
    {
      ""Key"": ""sproot"",
      ""Value"": ""dax""
    },
    {
      ""Key"": ""blen"",
      ""Value"": ""quar""
    },
    {
      ""Key"": ""nith"",
      ""Value"": ""glom""
    },
    {
      ""Key"": ""prax"",
      ""Value"": ""wend""
    },
    {
      ""Key"": ""zoril"",
      ""Value"": ""vemm""
    },
    {
      ""Key"": ""trindle"",
      ""Value"": ""fesk""
    },
    {
      ""Key"": ""marn"",
      ""Value"": ""cloop""
    },
    {
      ""Key"": ""velth"",
      ""Value"": ""snor""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 0,
  ""NewItems"": [
    {
      ""Key"": ""flarn"",
      ""Value"": ""mib""
    },
    {
      ""Key"": ""tovel"",
      ""Value"": ""crin""
    },
    {
      ""Key"": ""sproot"",
      ""Value"": ""dax""
    },
    {
      ""Key"": ""blen"",
      ""Value"": ""quar""
    },
    {
      ""Key"": ""nith"",
      ""Value"": ""glom""
    },
    {
      ""Key"": ""prax"",
      ""Value"": ""wend""
    },
    {
      ""Key"": ""zoril"",
      ""Value"": ""vemm""
    },
    {
      ""Key"": ""trindle"",
      ""Value"": ""fesk""
    },
    {
      ""Key"": ""marn"",
      ""Value"": ""cloop""
    },
    {
      ""Key"": ""velth"",
      ""Value"": ""snor""
    }
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );

            Assert.AreEqual(10, dut.Count);
            Assert.AreEqual(2, eventCount, "Expecting 2 multi-item events produced by AddRange().");

            // 2. Cancel Clear().
            localClearAll();

            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }
                ))
            {
                dut.Clear();
                Assert.AreEqual(10, dut.Count);
                Assert.AreEqual(1, eventCount, "Expecting 1 event Changing that gets cancelled.");

                // You can see the cancellation below.
                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
CancelRequested
A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": true
}

ThrowSoft
Type: OperationCanceledException
Id: OnCollectionChanging
OperationCanceledException"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );

            }

            // 3. Allow Clear().
            localClearAll();
            dut.Clear();
            Assert.AreEqual(0, dut.Count);
            Assert.AreEqual(2, eventCount, "Expecting 2 events (Changing + Changed).");

            // 4. Verify builder output.
            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestReplaceAndReplaceCancel()
        {
            localClearAll(includeDict: true);
            dut["Key1"] = "InitialValue";
            Assert.AreEqual(1, dut.Count);
            Assert.AreEqual(2, eventCount, "Expecting 2 events for Add.");

            localClearAll();
            dut["Key1"] = "ReplacedValue";
            Assert.AreEqual(1, dut.Count);
            Assert.AreEqual(2, eventCount, "Expecting 2 events for Replace.");

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": ""ReplacedValue""
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": ""InitialValue""
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": ""ReplacedValue""
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": ""InitialValue""
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match Replace event."
            );

            localClearAll();



            using (this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    isCancelRequested = true;
                },
                onDispose: (sender, e) =>
                {
                    isCancelRequested = false;
                }
                ))
            {
                dut["Key1"] = "CancelledValue";
            }
            Assert.AreEqual(1, dut.Count);
            Assert.AreEqual(1, eventCount, "Expecting 1 events Changing is canceled.");
            Assert.AreEqual("ReplacedValue", dut["Key1"]);
        }

        void subtestDHostEphemeral()
        {
            dut = new ObservableDictionary<int, string>();
            var prevMode = dut.Mode;
            using (dut.DHostEphemeralMode.GetToken(sender: DictionaryMode.TolerantCreateDefaultEntry))
            {
                Assert.AreEqual(DictionaryMode.TolerantCreateDefaultEntry, dut.Mode);
                using (dut.DHostEphemeralMode.GetToken(sender: DictionaryMode.InsistentNotNull))
                {
                    Assert.AreEqual(DictionaryMode.InsistentNotNull, dut.Mode);
                }
                Assert.AreEqual(DictionaryMode.TolerantCreateDefaultEntry, dut.Mode);
            }
            Assert.AreEqual(prevMode, dut.Mode);
        }
        #endregion S U B T E S T S
    }

    [TestMethod]
    public void Test_TolerantDictionary()
    {
        string actual, expected;
        List<string> builder = new();
        IDictionary dut;
        IObservableDictionary duto;

        subtestPatternMatchOnEmpty();
        subtestValueProvidedByEUD();
        subtestDHostEphemeral();
        subtestLogMissRequestByEUD();

        subtestDHostEphemeral();

        #region L o c a l F x 
        void localClear()
        {
            dut.Clear();
        }
        #endregion L o c a l F x

        #region S U B T E S T S

        // Normal tolerant return null behavior.
        void subtestPatternMatchOnEmpty()
        {
            dut = new TolerantDictionary<int, string>();
            Assert.AreEqual(0, dut.Count);
            if (dut[1] is { } value)
            {
                Assert.Fail($"Expecting value to be null, with no exception thrown.");
            }
        }

        // Normal tolerant behavior where EUD handles the CollectionChanging event.
        void subtestValueProvidedByEUD()
        {
            int countCoerced = 0;
            dut = duto = new TolerantDictionary<int, string>();

            #region L o c a l F x 
            using var local = duto.WithOnDispose(
                onInit: (sender, e) =>
                {
                    duto.CollectionChanging += localOnCollectionChanging;
                    duto.CollectionChanged += localOnCollectionChanged;
                },
                onDispose: (sender, e) =>
                {
                    duto.CollectionChanging -= localOnCollectionChanging;
                    duto.CollectionChanged -= localOnCollectionChanged;
                });

            void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
            {
                Assert.AreEqual(
                    NotifyCollectionChangingAction.Replace,
                    e.Action,
                    $"Expecting extended action flag for ePre.");


                builder.Add("P R E S E N T E D    T O    H A N D L E R");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                if (e.GetNewItemSingle() is DictionaryEntryPreview entry)
                {
                    entry.Value = $"Coerced.{++countCoerced:D2}";
                }
            }

            void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
            }
            #endregion L o c a l F x


            Assert.AreEqual(0, dut.Count);
            if (dut[1] is { } value)
            {
                Assert.AreEqual("Coerced.01", value);
                Assert.AreEqual(1, dut.Count, $"Expecting the new value has actually been ADDED not just RETURNED.");
            }
            else
            {
                Assert.Fail($"Expecting a value.");
            }
        }

        // Normal tolerant behavior where EUD handles the CollectionChanging
        // event by coercing a null entry that writes to @base.
        void subtestLogMissRequestByEUD()
        {
            dut = duto = new TolerantDictionary<int, string>();

            #region L o c a l F x 
            using var local = duto.WithOnDispose(
                onInit: (sender, e) =>
                {
                    duto.CollectionChanging += localOnCollectionChanging;
                },
                onDispose: (sender, e) =>
                {
                    duto.CollectionChanging -= localOnCollectionChanging;
                });

            void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
            {
                Assert.AreEqual(
                    NotifyCollectionChangingAction.Replace,
                    e.Action,
                    $"Expecting extended action flag for ePre.");


                builder.Add("P R E S E N T E D    T O    H A N D L E R");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                if (e.GetNewItemSingle() is DictionaryEntryPreview entry)
                {
                    entry.Value = TolerantValue.ExplicitNull;
                }
            }

            #endregion L o c a l F x

            Assert.IsFalse(
                duto.Contains(1),
                "Expecting key is NOT FOUND");

            using (duto.DHostEphemeralMode.GetToken(sender: DictionaryMode.TolerantCreateDefaultEntry))
            {
                Assert.AreEqual(
                    DictionaryMode.TolerantCreateDefaultEntry,
                    duto.Mode,
                    $"Expecting ephemeral mode is set.");
                object? expectingNull;


                Assert.AreEqual
                    (0,
                    dut.Count,
                    $"Expecting empty to start.");

                expectingNull = dut[1];

                // Pull a non-existent key
                Assert.IsNull(
                    expectingNull,
                    "Expecting the normal tolerant null behavior, except...");

                Assert.AreEqual(
                    1,
                    dut.Count,
                    $"Expecting the new null value has actually been ADDED not just RETURNED.");
                { }
            }

            Assert.IsTrue(
                duto.Contains(1),
                "Expecting key is FOUND as an effect of the tolerant mode");

            Assert.AreEqual(
                DictionaryMode.TolerantReturnDefault,
                duto.Mode,
                $"Expecting mode has reverted after block disposal.");
        }
        void subtestDHostEphemeral()
        {
            dut = duto = new TolerantDictionary<int, string>();
            var prevMode = duto.Mode;
            using (duto.DHostEphemeralMode.GetToken(sender: DictionaryMode.TolerantCreateDefaultEntry))
            {
                Assert.AreEqual(DictionaryMode.TolerantCreateDefaultEntry, duto.Mode);
                using (duto.DHostEphemeralMode.GetToken(sender: DictionaryMode.InsistentNotNull))
                {
                    Assert.AreEqual(DictionaryMode.InsistentNotNull, duto.Mode);
                }
                Assert.AreEqual(DictionaryMode.TolerantCreateDefaultEntry, duto.Mode);
            }

            Assert.AreEqual(prevMode, duto.Mode);
        }
        #endregion S U B T E S T S
    }

    [TestMethod]
    public void Test_InsistentDictionaryWithClass()
    {
        string actual, expected;
        List<string> builder = new();
        IDictionary dut;
        #region L o c a l F x 
        void localClear()
        {
            // Resart from instance 0;
            SimpleClass.ResetInstanceCount();
            dut = null!;
            builder.Clear();
        }
        #endregion L o c a l F x

        subtestDHostEphemeral();
        subtestAbstractWhenEmpty();

        subtestUnilateralContractWhenEmpty();
        subtestDefaultInferredActivatorForClass();


        #region S U B T E S T S
        void subtestDefaultInferredActivatorForClass()
        {
            localClear();

            // A simple class with a parameterless CTor 
            SimpleClass valueT;

            Framework.BriskReset();
            var dunk = Framework.Brisk[1]
                .AsStronglyTypedDictionary<string, SimpleClass>(
                activationDlgt: () => new SimpleClass());

            var duti = (IInsistentDictionary<string, SimpleClass>)dunk;

            dut = duti;

            // Default config
            Assert.IsInstanceOfType<Func<SimpleClass>>(duti.ActivationDlgt);

            Assert.AreEqual(0, dut.Count);
            valueT = dut["Key1"].SafeAs<SimpleClass>()!;
            Assert.IsNotNull(valueT);
        }

        void subtestUnilateralContractWhenEmpty()
        {
            localClear();

            // A simple class with a parameterless CTor
            SimpleClass valueT;

            Framework.BriskReset();

            var dunk = Framework.Brisk[1]
                .AsStronglyTypedDictionary<string, ISimpleClassUC>(
                DictionaryMode.InsistentNotNull);

            var duti = (IInsistentDictionary<string, ISimpleClassUC>)dunk;

            dut = duti;

            // "These days" there is no longer any inference of any kind.
            Assert.IsNull(duti.ActivationDlgt);

            Assert.AreEqual(0, dut.Count);
            valueT = dut["Key1"].SafeAs<SimpleClass>()!;
            Assert.IsNotNull(valueT);
        }

        void subtestAbstractWhenEmpty()
        {
            localClear();

            // A simple class with a parameterless CTor
            SimpleClass valueT;

            // But...the dictionary TValue is abstract (i.e. 'not' UC) in this case.
            var dunk = Framework.Brisk[1]
                .AsStronglyTypedDictionary<string, ISimpleClass>(
                DictionaryMode.InsistentNotNull);

            var duti = (IInsistentDictionary<string, ISimpleClass>)dunk;

            dut = duti;

            // In default config, the abstract will cause DAT to come up null.
            Assert.IsNull(duti.ActivationDlgt);
            { }

            #region L o c a l F x
            using var local = duti.WithOnDispose(
                onInit: (sender, e) =>
                {
                    duti.CollectionChanging += localOnCollectionChanging;
                    duti.CollectionChanged += localOnCollectionChanged;
                },
                onDispose: (sender, e) =>
                {
                    duti.CollectionChanging -= localOnCollectionChanging;
                    duti.CollectionChanged -= localOnCollectionChanged;
                });

            void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
            {
                Assert.IsTrue(
                    sender is IInsistent insistent);
                Assert.AreEqual(
                    NotifyCollectionChangingAction.Replace,
                    e.Action,
                    $"Expecting extended action flag for ePre.");

                builder.Add("P R E S E N T E D    T O    H A N D L E R");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));

                // YES: This *is* a dictionary and coercible *is* a cdep but we *don't* have to care.
                if (e.GetNewItemSingle() is DictionaryEntryPreview cvp)
                {
                    var status = e.NewItems.GetStatusAsList();
                    switch (status)
                    {
                        case StatusAsList.Single:
                            Assert.AreEqual(typeof(string), cvp.Key.GetType());
                            cvp.Value = new SimpleClass();
                            break;
                        case StatusAsList.Multi:
                            throw new NotImplementedException("ToDo");
                            break;
                        default:
                            Assert.Fail($"Unexpected status: {status.ToFullKey()}");
                            break;
                    }
                }
                else
                {
                    var cMe = e.GetNewItemSingle()?.GetType().Name;
                    Assert.Fail("That was supposed to work");
                }

                builder.Add("M O D I F I E D    I N    H A N D L E R");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                builder.AddEmpty();
            }
            void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                builder.Add("C O L L E C T I O N    C H A N G E D    B C L");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                builder.AddEmpty();
            }
            #endregion L o c a l F x


            Assert.AreEqual(0, dut.Count);
            valueT = dut["Key1"].SafeAs<SimpleClass>()!;
            Assert.IsNotNull(valueT);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
P R E S E N T E D    T O    H A N D L E R
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
M O D I F I E D    I N    H A N D L E R
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": {
        ""InstanceCount"": 1,
        ""TimeStamp"": ""1970-01-01T00:00:00+00:00""
      }
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

C O L L E C T I O N    C H A N G E D    B C L
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": {
        ""InstanceCount"": 1,
        ""TimeStamp"": ""1970-01-01T00:00:00+00:00""
      }
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );

            valueT = dut["Key2"].SafeAs<SimpleClass>()!;
            Assert.IsNotNull(valueT);


            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
P R E S E N T E D    T O    H A N D L E R
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
M O D I F I E D    I N    H A N D L E R
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": {
        ""InstanceCount"": 1,
        ""TimeStamp"": ""1970-01-01T00:00:00+00:00""
      }
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

C O L L E C T I O N    C H A N G E D    B C L
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": {
        ""InstanceCount"": 1,
        ""TimeStamp"": ""1970-01-01T00:00:00+00:00""
      }
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key1"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}

P R E S E N T E D    T O    H A N D L E R
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key2"",
      ""Value"": null
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key2"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
M O D I F I E D    I N    H A N D L E R
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key2"",
      ""Value"": {
        ""InstanceCount"": 2,
        ""TimeStamp"": ""1970-01-01T00:00:00+00:00""
      }
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key2"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

C O L L E C T I O N    C H A N G E D    B C L
{
  ""Action"": 2,
  ""NewItems"": [
    {
      ""Key"": ""Key2"",
      ""Value"": {
        ""InstanceCount"": 2,
        ""TimeStamp"": ""1970-01-01T00:00:00+00:00""
      }
    }
  ],
  ""OldItems"": [
    {
      ""Key"": ""Key2"",
      ""Value"": null
    }
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }

        void subtestDHostEphemeral()
        {
            var duti = Framework.Brisk[1]
                .AsStronglyTypedDictionary<int, string>(
                DictionaryMode.InsistentNotNull);

            dut = duti;
            var prevMode = duti.Mode;
            using (duti.DHostEphemeralMode.GetToken(sender: DictionaryMode.TolerantCreateDefaultEntry))
            {
                Assert.AreEqual(DictionaryMode.TolerantCreateDefaultEntry, duti.Mode);
                using (duti.DHostEphemeralMode.GetToken(sender: DictionaryMode.InsistentNotNull))
                {
                    Assert.AreEqual(DictionaryMode.InsistentNotNull, duti.Mode);
                }
                Assert.AreEqual(DictionaryMode.TolerantCreateDefaultEntry, duti.Mode);
            }
            Assert.AreEqual(prevMode, duti.Mode);
        }
        #endregion S U B T E S T S
    }

    [TestMethod]
    public void Test_KeyChainGeneration()
    {
        string actual, expected;
        var builder = new List<string>();

        subtest_SimpleAtomicKeys();
        subtest_FlattensNestedEnumerables();
        subtest_FormatsTemporalValues();
        subtest_FormatsGuidsAndUris();
        subtest_HandlesEnumAndTypeKeys();
        subtest_DeepFlatteningOfMixedCollections();
        subtest_HandlesNullAndEmptyEnumerables();
        subtest_FormatsNumbersInvariantly();
        subtest_FallbackForReferenceTypes();
        subtest_KeySegment();
        // Dependent on all the others.
        subtest_ShowAll();

        #region S U B T E S T S
        // 1. Basic string + int keys
        void subtest_SimpleAtomicKeys()
        {
            actual = new object[] { "Root", 42 }.MakePathFromObjects();
            builder.Add(actual);

            expected = Path.Combine("Root", "42");
            Assert.AreEqual(expected, actual, "Simple atomic key chain failed.");
        }

        // 2. Flattening of nested enumerables
        void subtest_FlattensNestedEnumerables()
        {
            var keyChain = "A".UnrollKeyChainObjects(new object[] { new[] { "B", "C" }, "D" });
            actual = string.Join(",", keyChain);
            builder.Add(actual);

            expected = "A,B,C,D";
            Assert.AreEqual(expected, actual, "Enumerable flattening failed.");
        }

        // 3. Temporal conversions (DateTime, DateOnly, TimeOnly, TimeSpan)
        void subtest_FormatsTemporalValues()
        {
            var dt = new DateTime(2025, 11, 11, 12, 30, 45, DateTimeKind.Utc);
            var date = new DateOnly(2025, 11, 11);
            var time = new TimeOnly(12, 30, 45);
            var span = new TimeSpan(1, 2, 3);
            actual = new object[] { dt, date, time, span }.MakePathFromObjects();
            builder.Add(actual);

            expected = Path.Combine("20251111T123045Z", "20251111", "123045", "T01-02-03");
            Assert.AreEqual(expected, actual, "Temporal normalization failed.");
        }

        // 4. GUIDs and URIs
        void subtest_FormatsGuidsAndUris()
        {
            var guid = Guid.Parse("01234567-89AB-CDEF-0123-456789ABCDEF");
            var uri = new Uri("https://example.com/x");
            actual = new object[] { guid, uri }.MakePathFromObjects();
            builder.Add(actual);

            expected = Path.Combine("0123456789ABCDEF0123456789ABCDEF", "https://example.com/x");
            Assert.AreEqual(expected, actual, "GUID/URI formatting failed.");
        }

        // 5. Enum and Type keys
        void subtest_HandlesEnumAndTypeKeys()
        {
            actual = new object[] { AttributeTargets.Class, typeof(string) }.MakePathFromObjects();
            builder.Add(actual);

            expected = Path.Combine("AttributeTargets.Class", "String");
            Assert.AreEqual(expected, actual, "Enum/Type conversion failed.");
        }

        // 6. Mixed flattening and nested IEnumerable
        void subtest_DeepFlatteningOfMixedCollections()
        {
            var nested = new List<object> { "B", new[] { "C1", "C2" }, "D" };
            var keyChain = "A".UnrollKeyChainObjects(nested);
            actual = string.Join(",", keyChain);
            builder.Add(actual);

            expected = "A,B,C1,C2,D";
            Assert.AreEqual(expected, actual, "Deep flattening failed.");
        }

        // 7. Handling of nulls and empty enumerables
        void subtest_HandlesNullAndEmptyEnumerables()
        {
            var keyChain = "A".UnrollKeyChainObjects(null!, Array.Empty<string>());
            actual = string.Join(",", keyChain);
            builder.Add(actual);

            expected = "A,"; // one null placeholder
            Assert.IsTrue(actual.StartsWith("A,"), "Null/empty handling mismatch.");
        }

        // 8. Culture-invariant numerics
        void subtest_FormatsNumbersInvariantly()
        {
            var number = 1234.56m;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            actual = new object[] { number }.MakePathFromObjects();
            builder.Add(actual);

            expected = "1234.56";
            Assert.AreEqual(expected, actual, "Invariant numeric formatting failed.");
        }

        // 9. Stable hash fallback for arbitrary reference types
        void subtest_FallbackForReferenceTypes()
        {
            var obj = new object();
            actual = new object[] { obj }.MakePathFromObjects();
            builder.Add(actual);

            Assert.IsTrue(actual.StartsWith("Object:", StringComparison.Ordinal), "Fallback naming not applied.");
            Assert.IsTrue(int.TryParse(actual.Split(':')[1], out _), "HashCode not appended.");
        }

        // 10. Absolute and Relative Key Segments
        void subtest_KeySegment()
        {
            actual = new object[] { StdAbsoluteKeyDefault.SimpleClass }.MakePathFromObjects();
            builder.Add(actual);

            actual.ToClipboardExpected();
            { }
            expected = @" 
StdAbsoluteKeyDefault\SimpleClass";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting enum type + member."
            );

            actual = new object[] { StdAbsoluteKeyWithString.SimpleClass }.MakePathFromObjects();
            builder.Add(actual);

            actual.ToClipboardExpected();
            { }
            expected = @" 
StdAbsoluteKeyWithString\Level1\SimpleClass"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting enum type + @string + member."
            );

            actual = new object[] { StdAbsoluteKeyWithType.SimpleClass }.MakePathFromObjects();
            builder.Add(actual);

            actual.ToClipboardExpected();
            { }
            expected = @" 
StdAbsoluteKeyWithType\Type\Object\SimpleClass"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting path to match"
            );

            actual = new object[] { StdCacheReflectionStrongTyped.SimpleClass }.MakePathFromObjects();
            builder.Add(actual);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Type\Object\SimpleClass\Classes"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 1. No Root 2. Member rel appended"
            );

            actual = new object[] { StdCacheReflectionStrongTyped.ButtonWindowsForms }.MakePathFromObjects();
            builder.Add(actual);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Type\Object\ButtonWindowsForms\Platform\Buttons"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 1. No Root 2. Member rel appended"
            );
        }

        void subtest_ShowAll()
        {
            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Root\42
A,B,C,D
20251111T123045Z\20251111\123045\T01-02-03
0123456789ABCDEF0123456789ABCDEF\https://example.com/x
AttributeTargets.Class\String
A,B,C1,C2,D
A,
1234.56
Object:22599820
StdAbsoluteKeyDefault\SimpleClass
StdCacheReflectionB\Level1\SimpleClass
StdCacheReflectionC\Type\Object\SimpleClass
Type\Object\SimpleClass"
            ;

#if false
            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting ThrowHard."
            );
#endif
        }
        #endregion S U B T E S T S
    }
}
