using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.Collections.MSTest.OPC;
using IVSoftware.Portable.Collections.MSTest.TestTargets;
using IVSoftware.Portable.Collections.MSTest.TestUtils;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.Threading;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.WinOS.MSTest.Extensions;
using Newtonsoft.Json;
using SQLite;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections.MSTest
{
    [TestClass]
    public class TestClass_ObservablePreviewCollection
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
        static int IncrementCount(ref int count)
            => ++count;

        [TestMethod]
        public void Test_ObservablePreviewCollection()
        {
            List<string> builder = new();

            string actual, expected;
            bool isCoerceRequested = false;
            bool isCancelRequested = false;
            // [Careful] Use IncrementCount to modify these.
            int
                eventCount = 0,
                coercedValueCount = 0;

            var lutc = new ObservablePreviewCollection<string>();
            IList lut = lutc;
            #region L o c a l F x
            bool clearToggle = false;
            void localClear(bool includeLut)
            {
                if (includeLut)
                {
                    // Toggle shares the love betwee IList and IList<T>
                    clearToggle = !clearToggle;
                    if (clearToggle)
                    {
                        lut.Clear();
                    }
                    else
                    {
                        lutc.Clear();
                    }
                }
                builder.Clear();
                isCoerceRequested = isCancelRequested = false;
                coercedValueCount = eventCount = 0;
            }
            // For the duration of this test.
            using var local = lutc.WithOnDispose(
                onInit: (sender, e) =>
                {
                    lutc.CollectionChanging += localOnCollectionChanging;
                    lutc.CollectionChanged += localOnCollectionChanged;
                    Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
                },
                onDispose: (sender, e) =>
                {
                    lutc.CollectionChanging -= localOnCollectionChanging;
                    lutc.CollectionChanged -= localOnCollectionChanged;
                    Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
                });

            void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
            {
                IncrementCount(ref eventCount);

                builder.Add($"Event = {eventCount:D3}");

                builder.Add($"C O L L E C T I O N    C H A N G I N G    E V E N T    B C L");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                builder.AddEmpty();
                if (isCancelRequested)
                {
                    builder.Add("CancelRequested");
                    e.Cancel = true;
                }
                if (isCoerceRequested)
                {
                    for (int i = 0; i < e.NewItems?.Count; i++)
                    {
                        e.NewItems[i] = $"{DateTimeOffset.UnixEpoch} {e.NewItems[i]?.ToString()}";
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
                IncrementCount(ref eventCount);

                builder.Add($"Event = {eventCount:D3}");

                builder.Add($"C O L L E C T I O N    C H A N G E D    E V E N T    B C L");
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                builder.Add(string.Empty);
            }


            void localOnBeginThrowOrAdvise(object? sender, Throw e)
            {
                builder.Add($"{e.ToString(ThrowToStringFormat.MSTest)}");
                e.Handled = true;
            }
            #endregion L o c a l F x

            // Utility - What happend if object is not T?
            subtestCastException();

            subtestClear();
            subtestAdd();
            subtestAddCoerce();
            subtesAddRange();
            subtestInsert();
            subtestInsertRange();

            subtestRemove();
            subtestReplace();
            subtestMove();
            subtestCancelAdd();
            subtestCancelInsert();


            #region S U B T E S T S

            void subtestCastException()
            {
                localClear(includeLut: true);
                lut.Add((uint)1);

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
ThrowHard
Type: InvalidCastException
Id: Add
Invalid cast in Add(object?)."
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
            }

            void subtestClear()
            {
                localClear(includeLut: true);

                // POPULATE: Add some items to test against.
                lutc.AddRange(
                new[]
                {
                    "Alpha.1",
                    "Bravo.1",
                    "Charlie.1",
                    "Delta.1",
                    "Echo.1",
                });
                localClear(includeLut: false);

                builder.Clear();

                lut.Clear();

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[]"
                ;
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [
    ""Alpha.1"",
    ""Bravo.1"",
    ""Charlie.1"",
    ""Delta.1"",
    ""Echo.1""
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [
    ""Alpha.1"",
    ""Bravo.1"",
    ""Charlie.1"",
    ""Delta.1"",
    ""Echo.1""
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting serialization shows items that were reset."
                );
            }

            void subtestAdd()
            {
                localClear(includeLut: true);
                lut.Add("Alpha");

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha""
]";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting one item."
                );

                lut.Add("Bravo");

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha"",
  ""Bravo""
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );


                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}

Event = 003
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Bravo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 004
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Bravo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting event sequence."
                );
            }

            void subtestAddCoerce()
            {
                localClear(includeLut: true);
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
                    lut.Add($"Coerced.{IncrementCount(ref coercedValueCount):D2}");

                    actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
[
  ""1/1/1970 12:00:00 AM +00:00 Coerced.01""
]"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting json serialization to match."
                    );


                    actual = string.Join(Environment.NewLine, builder);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Coerced.01""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 0,
  ""NewItems"": [
    ""1/1/1970 12:00:00 AM +00:00 Coerced.01""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""1/1/1970 12:00:00 AM +00:00 Coerced.01""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting event sequence to match."
                    );
                }
            }

            void subtesAddRange()
            {
                localClear(includeLut: true);

                // B A T C H    F A L S E
                lutc.AddRange(
                    new[]
                    {
                        "Alpha",
                        "Bravo",
                        "Charlie",
                        "Delta",
                        "Echo",
                    });

                // ^^^^^^^^^
                // [Careful]
                // - This *is* the test (not pre-population).
                // - Do 'not' clear builder. Not even a little bit.

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha"",
  ""Bravo"",
  ""Charlie"",
  ""Delta"",
  ""Echo""
]";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha"",
    ""Bravo"",
    ""Charlie"",
    ""Delta"",
    ""Echo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha"",
    ""Bravo"",
    ""Charlie"",
    ""Delta"",
    ""Echo""
  ],
  ""OldItems"": null,
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

                localClear(includeLut: true);

                lutc.AddRange(
                    new[]
                    {
                        "Alpha",
                        "Bravo",
                        "Charlie",
                        "Delta",
                        "Echo",
                    });


                // ^^^^^^^^^
                // [Careful]
                // - This *is* the test (not pre-population).
                // - Do 'not' clear builder. Not even a little bit.


                actual = JsonConvert.SerializeObject(lutc, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha"",
  ""Bravo"",
  ""Charlie"",
  ""Delta"",
  ""Echo""
]";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                // C A N C E L    T R U E
                // [Careful]
                // You must do the utility clear BEFORE checking out the bool.
                localClear(includeLut: true);

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

                    lutc.AddRange(
                        new[]
                        {
                            "Alpha",
                            "Bravo",
                            "Charlie",
                            "Delta",
                            "Echo",
                        });
                    { }

                    actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
[]"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting EMPTY list due to cancel"
                    );

                    actual = string.Join(Environment.NewLine, builder);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha"",
    ""Bravo"",
    ""Charlie"",
    ""Delta"",
    ""Echo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

CancelRequested
A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha"",
    ""Bravo"",
    ""Charlie"",
    ""Delta"",
    ""Echo""
  ],
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
                        "Expecting 5 DISCRETE CANCELED events."
                    );
                }

                // C O E R C E    T R U E
                localClear(includeLut: true);

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
                    lutc.AddRange(
                        new[]
                        {
                            "Alpha",
                            "Bravo",
                            "Charlie",
                            "Delta",
                            "Echo",
                        });

                    actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
[
  ""1/1/1970 12:00:00 AM +00:00 Alpha"",
  ""1/1/1970 12:00:00 AM +00:00 Bravo"",
  ""1/1/1970 12:00:00 AM +00:00 Charlie"",
  ""1/1/1970 12:00:00 AM +00:00 Delta"",
  ""1/1/1970 12:00:00 AM +00:00 Echo""
]"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting ALL items to show evidence or coercion"
                    );

                    actual = string.Join(Environment.NewLine, builder);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha"",
    ""Bravo"",
    ""Charlie"",
    ""Delta"",
    ""Echo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 0,
  ""NewItems"": [
    ""1/1/1970 12:00:00 AM +00:00 Alpha"",
    ""1/1/1970 12:00:00 AM +00:00 Bravo"",
    ""1/1/1970 12:00:00 AM +00:00 Charlie"",
    ""1/1/1970 12:00:00 AM +00:00 Delta"",
    ""1/1/1970 12:00:00 AM +00:00 Echo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""1/1/1970 12:00:00 AM +00:00 Alpha"",
    ""1/1/1970 12:00:00 AM +00:00 Bravo"",
    ""1/1/1970 12:00:00 AM +00:00 Charlie"",
    ""1/1/1970 12:00:00 AM +00:00 Delta"",
    ""1/1/1970 12:00:00 AM +00:00 Echo""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1
}
";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting builder content to match."
                    );
                }
            }

            void subtestInsert()
            {
                // POPULATE: Add some items to test against.
                localClear(includeLut: true);
                lutc.AddRange(
                    new[]
                    {
                        "Alpha.1",
                        "Bravo.1",
                        "Charlie.1",
                        "Delta.1",
                        "Echo.1",
                    });
                localClear(includeLut: false);

                var insertAt = 0;
                foreach (
                    var item in
                    new[]
                    {
                        "Alpha.0",
                        "Alpha.2",
                        "Bravo.2",
                        "Charlie.2",
                        "Delta.2",
                        "Echo.2",
                    })
                {
                    lut.Insert(insertAt, item);
                    insertAt += 2;
                }

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha.0"",
  ""Alpha.1"",
  ""Alpha.2"",
  ""Bravo.1"",
  ""Bravo.2"",
  ""Charlie.1"",
  ""Charlie.2"",
  ""Delta.1"",
  ""Delta.2"",
  ""Echo.1"",
  ""Echo.2""
]";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha.0""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha.0""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1
}

Event = 003
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 2,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 004
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Alpha.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 2,
  ""OldStartingIndex"": -1
}

Event = 005
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Bravo.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 4,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 006
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Bravo.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 4,
  ""OldStartingIndex"": -1
}

Event = 007
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Charlie.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 6,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 008
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Charlie.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 6,
  ""OldStartingIndex"": -1
}

Event = 009
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Delta.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 8,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 010
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Delta.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 8,
  ""OldStartingIndex"": -1
}

Event = 011
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Echo.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 10,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 012
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Echo.2""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 10,
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
            void subtestInsertRange()
            {
                // POPULATE: Add some items to test against.
                localClear(includeLut: true);

                lutc.AddRange(
                    new[]
                    {
                        "Alpha.1",
                        "Bravo.1",
                        "Charlie.1",
                        "Delta.1",
                        "Echo.1",
                    });

                localClear(includeLut: false);

                var insertAt = 0;
                foreach (
                    var item in
                    new[]
                    {
                        "Alpha.0",
                        "Alpha.2",
                        "Bravo.2",
                        "Charlie.2",
                        "Delta.2",
                        "Echo.2",
                    })
                {
                    lut.Insert(insertAt, item);
                    insertAt += 2;
                }
            }

            void subtestRemove()
            {
                // POPULATE: Add some items to test against.
                localClear(includeLut: true);
                lutc.AddRange(
                    new[]
                    {
                        "Alpha.1",
                        "Bravo.1",
                        "Charlie.1",
                        "Delta.1",
                        "Echo.1",
                    });
                localClear(includeLut: false);

                lut.Remove("Charlie.1");

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha.1"",
  ""Bravo.1"",
  ""Delta.1"",
  ""Echo.1""
]";
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    ""Charlie.1""
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    ""Charlie.1""
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

            void subtestReplace()
            {
                // POPULATE: Add some items to test against.
                localClear(includeLut: true);
                lutc.AddRange(
                    new[]
                    {
                        "Alpha.1",
                        "Bravo.1",
                        "Charlie.1",
                        "Delta.1",
                        "Echo.1",
                    });
                localClear(includeLut: false);

                lut[2] = "Charlie.2";

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha.1"",
  ""Bravo.1"",
  ""Charlie.2"",
  ""Delta.1"",
  ""Echo.1""
]";
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 2,
  ""NewItems"": [
    ""Charlie.2""
  ],
  ""OldItems"": [
    ""Charlie.1""
  ],
  ""NewStartingIndex"": 2,
  ""OldStartingIndex"": 2,
  ""Cancel"": false
}

Event = 002
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 2,
  ""NewItems"": [
    ""Charlie.2""
  ],
  ""OldItems"": [
    ""Charlie.1""
  ],
  ""NewStartingIndex"": 2,
  ""OldStartingIndex"": 2
}
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
            }

            void subtestMove()
            {
                localClear(includeLut: true);
                lutc.AddRange(
                    new[]
                    {
                        "Alpha.1",
                        "Bravo.1",
                        "Charlie.1",
                        "Delta.1",
                        "Echo.1",
                    });

                builder.Clear();

                ((IObservablePreviewCollection)lut).Move(4, 0);


                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Echo.1"",
  ""Alpha.1"",
  ""Bravo.1"",
  ""Charlie.1"",
  ""Delta.1""
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 003
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 3,
  ""NewItems"": [
    ""Echo.1""
  ],
  ""OldItems"": [
    ""Echo.1""
  ],
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": 4,
  ""Cancel"": false
}

Event = 004
C O L L E C T I O N    C H A N G E D    E V E N T    B C L
{
  ""Action"": 3,
  ""NewItems"": [
    ""Echo.1""
  ],
  ""OldItems"": [
    ""Echo.1""
  ],
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": 4
}
"
                ;
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
            }

            void subtestCancelAdd()
            {
                // POPULATE: Add some items to test against.
                localClear(includeLut: true);
                lutc.AddRange(
                new[]
                {
                    "Alpha.1",
                    "Bravo.1",
                });
                localClear(includeLut: false);


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
                    lut.Add("Charlie.Cancel");
                }

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha.1"",
  ""Bravo.1""
]";
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Charlie.Cancel""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

CancelRequested
A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Charlie.Cancel""
  ],
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

            void subtestCancelInsert()
            {
                // POPULATE: Add some items to test against.
                localClear(includeLut: true);
                lutc.AddRange(
                new[]
                {
                    "Alpha.1",
                    "Bravo.1",
                    "Charlie.1",
                });
                localClear(includeLut: false);


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
                    lut.Insert(1, "Bravo.Cancel");
                }

                actual = JsonConvert.SerializeObject(lut, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  ""Alpha.1"",
  ""Bravo.1"",
  ""Charlie.1""
]";
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting json serialization to match."
                );

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
Event = 001
C O L L E C T I O N    C H A N G I N G    E V E N T    B C L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Bravo.Cancel""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

CancelRequested
A F T E R    C O E R C E    O R    C A N C E L
{
  ""Action"": 0,
  ""NewItems"": [
    ""Bravo.Cancel""
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 1,
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

            #endregion
        }

        [TestMethod]
        public async Task Test_PredicateList()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            OPCItem
                itemUnselected = new OPCItem { IsSelected = false },
                itemSelected = new OPCItem { IsSelected = true };

            var lutc = new ObservablePreviewCollection<OPCItem>();

            lutc.TrackContexts.Track(lutc, nameof(OPCItem.IsSelected));

            Assert.IsTrue(lutc.OptimizationMode.HasFlag(ListOptimizationMode.TrackItemPropertyChanges));
            lutc.AddRange([itemUnselected, itemSelected]);

            Assert.AreEqual(lutc.Count, 2, "Expecting unfiltered list of 2");

            lutc.ActivateFilters(TestPredicate.IsSelected);
            await lutc;

            var sbFiltered = lutc.ToArray();
            { }
            Assert.AreEqual(lutc.Count, 1, "Expecting filtered list of 1");
            { }

            //Assert.AreEqual(lutc.Count, 1, $"Expecting one not-selected item ");
            //Assert.AreEqual(filteredList.Count, 0, $"Expecting no matches.");

            //Assert.AreEqual(lutc.Count, 2, $@"Expecting ""one of each"". ");
            //Assert.AreEqual(filteredList.Count, 1, $"Expecting only one of them to match.");

            //lutc.Remove(itemSelected);
            //Assert.AreEqual(lutc.Count, 1, $"Expecting one not-selected item ");
            //Assert.AreEqual(filteredList.Count, 0, $"Expecting no matches.");
        }

        [TestMethod]
        public void Test_API()
        {
            string actual, expected;
            int lutId = 0, itemCount = 100;
            var builder = new List<string>();

            IList[] luts = [];

            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
                    Threading.Extensions.Awaited += localOnAwaited;
                },
                onDispose: (sender, e) =>
                {
                    Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
                    Threading.Extensions.Awaited -= localOnAwaited;
                });

            localMakeLuts(
            [
                new ObservablePreviewCollection<int>(){ Mode = ListMode.Normal },
                new ObservablePreviewCollection<int>(){ Mode = ListMode.TolerantReturnDefault },
                new ObservablePreviewCollection<int>(){ Mode = ListMode.TolerantCreateDefaultEntry },
                new ObservablePreviewCollection<int?>(){ Mode = ListMode.Normal },
                new ObservablePreviewCollection<int?>(){ Mode = ListMode.TolerantReturnDefault },
                new ObservablePreviewCollection<int?>(){ Mode = ListMode.TolerantCreateDefaultEntry },
            ]);

            #region L o c a l   F x

            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                switch (e.Caller)
                {
                    case "Item":
                        break;
                    default:
                        break;
                }
            }
            void localClearAll()
            {
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = luts[lutId];
                    lut.Clear();
                }
                itemCount = 100;
                builder.Clear();
            }

            void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
            {
                builder.Add(localHeader("E P R E"));
                builder.Add(JsonConvert.SerializeObject(e, Formatting.Indented));
                builder.AddEmpty();
            }

            void localOnAnyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                builder.Add(localHeader("C H A N G E D    L I S T"));
                builder.Add(JsonConvert.SerializeObject(sender, Formatting.Indented));
                builder.AddEmpty();
            }

            void localOnBeginThrowOrAdvise(object? sender, Throw e)
            {
                builder.Add(localHeader("E X C E P T I O N"));
                builder.Add($"{e.Mode}: {e.FormattedMessage}");
                builder.AddEmpty();
                e.Handled = true;
            }

            string localHeader(string header)
                => $"[Lut {lutId:D2} {(luts[lutId] as IObservablePreviewCollection)?.Mode.ToFullKey()}] {header}";

            void localMakeLuts(IList[] newConfig)
            {
                foreach (var incc in luts.OfType<INotifyCollectionChanged>())
                {
                    if (incc is INotifyCollectionChanging inpcc)
                    {
                        inpcc.CollectionChanging -= localOnCollectionChanging;
                    }
                    incc.CollectionChanged -= localOnAnyCollectionChanged;
                }
                luts = newConfig;
                foreach (var incc in luts.OfType<INotifyCollectionChanged>())
                {
                    if (incc is INotifyCollectionChanging inpcc)
                    {
                        inpcc.CollectionChanging += localOnCollectionChanging;
                    }
                    incc.CollectionChanged += localOnAnyCollectionChanged;
                }
            }

            static void localPopulateListWithRandom(
                IList list,
                int N,
                int Min,
                int Max,
                bool distinct = false,
                bool append = false,
                int seed = 1)
            {
                Random rando;
                rando = new Random(seed);
                if (append)
                {
                    N += list.Count;
                }
                else
                {
                    list.Clear();
                }
                while (list.Count < N)
                {
                    if (distinct)
                    {
                        if (list is IObservablePreviewCollection opcList)
                        {
                            opcList.AddDistinct(rando.Next(Min, Max));
                        }
                        else
                        {
                            throw new NotImplementedException("ToDo: Write extension for IList");
                        }
                    }
                    else
                    {
                        list.Add(rando.Next(Min, Max));
                    }
                }
            }
            #endregion

            bool forceUpcast = true;
            // IList (non-generic) members
            subtest_IList_GetIndexerWithOptions();
            subtest_IList_SetIndexerWithOptions();
            subtest_IList_Add();
            subtest_IList_Clear();
            subtest_IList_Contains();
            subtest_IList_CopyTo();
            subtest_IList_GetEnumerator();
            subtest_IList_IndexOf();
            subtest_IList_Insert();
            subtest_IList_Remove();
            subtest_IList_RemoveAt();

            forceUpcast = true;
            // IListT (generic) members
            subtest_IList_GetIndexerWithOptions();
            subtest_IList_SetIndexerWithOptions();
            subtest_IList_Add();
            subtest_IList_Clear();
            subtest_IList_Contains();
            subtest_IList_CopyTo();
            subtest_IList_GetEnumerator();
            subtest_IList_IndexOf();
            subtest_IList_Insert();
            subtest_IList_Remove();
            subtest_IList_RemoveAt();

            #region S U B T E S T S

            void subtest_IList_GetIndexerWithOptions()
            {

                #region L o c a l F x 
                using var local = this.WithOnDispose(
                    onInit: (sender, e) =>
                        {
                            IVSoftware.Portable.Threading.Extensions.Awaited += localOnAwaited;
                        },
                    onDispose: (sender, e) =>
                        {
                            IVSoftware.Portable.Threading.Extensions.Awaited -= localOnAwaited;
                        });
                void localOnAwaited(object? sender, AwaitedEventArgs e)
                {
                    var preview = e.Caller;
                    switch (preview)
                    {
                        case "object? this[int index]":
                            break;
                        case "IList<T>.this[int index]":
                            break;
                        default:
                            break;
                    }
                }
                #endregion L o c a l F x


                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = luts[lutId];
                    _ = lut[0];
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E X C E P T I O N
ThrowHard: Item | IndexOutOfRangeException

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 2,
  ""NewItems"": [
    null
  ],
  ""OldItems"": [
    null
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 2,
  ""NewItems"": [
    null
  ],
  ""OldItems"": [
    null
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] E X C E P T I O N
ThrowHard: Item | IndexOutOfRangeException

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 2,
  ""NewItems"": [
    null
  ],
  ""OldItems"": [
    null
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 2,
  ""NewItems"": [
    null
  ],
  ""OldItems"": [
    null
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );

                { }
                builder.Clear();

                // [Canonical] Test BOTH Interfaces
                // Try with generic interface.
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    if (luts[lutId].SafeAs<IList<int>>() is { } notNull)
                    {
                        IList<int> lut = notNull;
                        _ = lut[0];
                    }
                    else if (luts[lutId].SafeAs<IList<int?>>() is { } nullable)
                    {
                        IList<int?> lut = nullable;
                        _ = lut[0];
                    }
                    else
                    {
                        Assert.Fail($"Expecting successful case from {luts[lutId].GetType().FullName}");
                    }
                }

                // Should not change
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
            }

            void subtest_IList_SetIndexerWithOptions()
            {
                localClearAll();

                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = luts[lutId];
                    lut[0] = (100 * (1 + lutId)) + itemCount++;
                }


                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }

                expected = @" 
[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    200
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[
  200
]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    301
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  301
]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    402
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  402
]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    503
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[
  503
]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    604
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  604
]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    705
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  705
]
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match."
                );
            }

            void subtest_IList_Add()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = luts[lutId];
                    lut.Add(1);
                    lut.Add(2);
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[
  1
]

[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    2
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": fals
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[
  1,
  2
]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1
]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    2
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1,
  2
]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1
]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    2
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1,
  2
]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[
  1
]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    2
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[
  1,
  2
]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1
]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    2
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1,
  2
]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1
]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    2
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1,
  2
]
"
                ;
            }

            void subtest_IList_Clear()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = (IList)luts[lutId];
                    lut.Clear();
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 4,
  ""NewItems"": null,
  ""OldItems"": [],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[]
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.Clear."
                );
            }

            void subtest_IList_Contains()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = (IList)luts[lutId];
                    localCheckRandomContains(lut);
                }
                static void localCheckRandomContains(IList list)
                {
                    string actual, expected;

                    var builder = new List<string>();
                    var rando = new Random(1);

                    var actions = new[]
                    {
                        NotifyCollectionChangingAction.Reset,
                        NotifyCollectionChangingAction.Add,
                        NotifyCollectionChangingAction.Remove,
                    };

                    for (int i = 1; i <= 10; i++)
                    {
                        list.Add(i);
                    }
                    for (int i = 0; i < 25; i++)
                    {
                        var n = rando.Next(25);
                        builder.Add($"{n}: {list.Contains(n)}");
                    }

                    actual = string.Join(Environment.NewLine, builder);
                    actual.ToClipboardAssert("Expecting accurate predicate result.");
                    { }
                    expected = @" 
6: True
2: True
11: False
19: False
16: False
10: True
8: True
23: False
2: True
16: False
0: False
6: True
8: True
24: False
17: False
16: False
7: True
15: False
17: False
17: False
23: False
2: True
4: True
9: True
19: False";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting accurate predicate result."
                    );
                }
            }

            void subtest_IList_CopyTo()
            {
                localClearAll();
                var localBuilder = new List<string>();

                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = (IList)luts[lutId];
                    localBuilder.Add(lut.GetType().ToFormattedTypeName());
                    { }
                    ((IObservablePreviewCollection)lut).AddRange(Enumerable.Range(1, 10).Select(_ => _));
                    if (Nullable.GetUnderlyingType(lut.GetType().GetGenericArguments()![0]) is null)
                    {
                        var buffer = new int[15];
                        lut.CopyTo(buffer, 5);
                        actual = JsonConvert.SerializeObject(buffer);
                        actual.ToClipboardExpected();
                        { }
                        expected = @" 
[0,0,0,0,0,1,2,3,4,5,6,7,8,9,10]"
                        ;
                    }
                    else
                    {
                        var buffer = new int?[15];
                        lut.CopyTo(buffer, 5);
                        actual = JsonConvert.SerializeObject(buffer);
                        actual.ToClipboardExpected();
                        { }
                        expected = @" 
[null,null,null,null,null,1,2,3,4,5,6,7,8,9,10]"
                        ;
                    }
                    localBuilder.Add(actual);

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting json serialization to match."
                    );
                }

                actual = string.Join(Environment.NewLine, localBuilder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
IVSoftware.Portable.Collections.Lists.ObservablePreviewCollection<System.Int32>
[0,0,0,0,0,1,2,3,4,5,6,7,8,9,10]
IVSoftware.Portable.Collections.Lists.ObservablePreviewCollection<System.Int32>
[0,0,0,0,0,1,2,3,4,5,6,7,8,9,10]
IVSoftware.Portable.Collections.Lists.ObservablePreviewCollection<System.Int32>
[0,0,0,0,0,1,2,3,4,5,6,7,8,9,10]
IVSoftware.Portable.Collections.Lists.ObservablePreviewCollection<System.Nullable<System.Int32>>
[null,null,null,null,null,1,2,3,4,5,6,7,8,9,10]
IVSoftware.Portable.Collections.Lists.ObservablePreviewCollection<System.Nullable<System.Int32>>
[null,null,null,null,null,1,2,3,4,5,6,7,8,9,10]
IVSoftware.Portable.Collections.Lists.ObservablePreviewCollection<System.Nullable<System.Int32>>
[null,null,null,null,null,1,2,3,4,5,6,7,8,9,10]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting aggregate serialization to match."
                );
            }

            void subtest_IList_GetEnumerator()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = (IList)luts[lutId];
                    Assert.IsNotNull(lut.GetEnumerator());
                }
            }

            void subtest_IList_IndexOf()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = luts[lutId];
                    localIndexOfLoopback(lut);
                }
                static void localIndexOfLoopback(IList list)
                {
                    string actual, expected;

                    var builder = new List<string>();

                    localPopulateListWithRandom(list, 25, 100, 200);

                    // DFWI
                    Assert.AreEqual(
                        25,
                        list.Count,
                        $"This actually would have failed when we were conflating the 'count of the distinct hash set' with Count.");


                    actual = JsonConvert.SerializeObject(list, Formatting.Indented);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
[
  124,
  111,
  146,
  177,
  165,
  143,
  135,
  194,
  110,
  164,
  102,
  124,
  132,
  198,
  168,
  165,
  128,
  161,
  170,
  170,
  194,
  109,
  116,
  138,
  179
]";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting json serialization to match."
                    );

                    actual = string.Join(Environment.NewLine, list.OfType<object?>().Select(_ => $"{list.IndexOf(_):D2}. {_}"));
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
00. 124
01. 111
02. 146
03. 177
04. 165
05. 143
06. 135
07. 194
08. 110
09. 164
10. 102
00. 124
12. 132
13. 198
14. 168
04. 165
16. 128
17. 161
18. 170
18. 170
07. 194
21. 109
22. 116
23. 138
24. 179"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting IndexOf finds the FIRST index of a value."
                    );

                    // StartOver


                    localPopulateListWithRandom(list, 25, 100, 200, distinct: true);

                    actual = JsonConvert.SerializeObject(list, Formatting.Indented);
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
[
  124,
  111,
  146,
  177,
  165,
  143,
  135,
  194,
  110,
  164,
  102,
  132,
  198,
  168,
  128,
  161,
  170,
  109,
  116,
  138,
  179,
  130,
  182,
  188,
  155
]"
                    ;

                    actual = string.Join(Environment.NewLine, list.OfType<object?>().Select(_ => $"{list.IndexOf(_):D2}. {_}"));
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
00. 124
01. 111
02. 146
03. 177
04. 165
05. 143
06. 135
07. 194
08. 110
09. 164
10. 102
11. 132
12. 198
13. 168
14. 128
15. 161
16. 170
17. 109
18. 116
19. 138
20. 179
21. 130
22. 182
23. 188
24. 155"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting monotonicity due to distinct."
                    );
                }
            }

            void subtest_IList_Insert()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    if (luts[lutId].SafeAs<IList<int>>() is { } notNull)
                    {
                        IList<int> lut = notNull;
                        lut.Insert(0, 1);
                    }
                    else if (luts[lutId].SafeAs<IList<int?>>() is { } nullable)
                    {
                        IList<int?> lut = nullable;
                        lut.Insert(0, 1);
                    }
                    else
                    {
                        Assert.Fail($"Expecting successful case from {luts[lutId].GetType().FullName}");
                    }
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[
  1
]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1
]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1
]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[
  1
]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1
]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1
]
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.Insert."
                );

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.Contains."
                );
            }

            void subtest_IList_Remove()
            {
                localClearAll();
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    var lut = (IList)luts[lutId];
                    lut.Remove(1);
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[]
"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.Remove."
                );
            }

            void subtest_IList_RemoveAt()
            {
                localClearAll();

                // [Canonical] Test BOTH Interfaces
                // Try with generic interface.
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    if (luts[lutId].SafeAs<IList<int>>() is { } notNull)
                    {
                        IList<int> lut = notNull;
                        lut.RemoveAt(0);
                    }
                    else if (luts[lutId].SafeAs<IList<int?>>() is { } nullable)
                    {
                        IList<int?> lut = nullable;
                        lut.RemoveAt(0);
                    }
                    else
                    {
                        Assert.Fail($"Expecting successful case from {luts[lutId].GetType().FullName}");
                    }
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                actual.ToClipboardAssert("Expecting builder content to match after IList.RemoveAt.");
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 01 ListMode.TolerantReturnDefault] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 02 ListMode.TolerantCreateDefaultEntry] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 03 ListMode.Normal] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 04 ListMode.TolerantReturnDefault] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 05 ListMode.TolerantCreateDefaultEntry] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException
";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.RemoveAt."
                );



                // [Canonical] Test BOTH Interfaces
                // Try with generic interface.
                for (lutId = 0; lutId < luts.Length; lutId++)
                {
                    if (luts[lutId].SafeAs<IList<int>>() is { } notNull)
                    {
                        IList<int> lut = notNull;
                        lut[0] = 1000;
                        lut.RemoveAt(0);
                    }
                    else if (luts[lutId].SafeAs<IList<int?>>() is { } nullable)
                    {
                        IList<int?> lut = nullable;
                        lut[0] = 1000;
                        lut.RemoveAt(0);
                    }
                    else
                    {
                        Assert.Fail($"Expecting successful case from {luts[lutId].GetType().FullName}");
                    }
                }

                actual = string.Join(Environment.NewLine, builder);
                actual.ToClipboardExpected();
                { }
                actual.ToClipboardAssert("Expecting builder content to match after IList.RemoveAt.");
                { }
                expected = @" 
[Lut 00 ListMode.Normal] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 01 ListMode.TolerantReturnDefault] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 02 ListMode.TolerantCreateDefaultEntry] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 03 ListMode.Normal] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 04 ListMode.TolerantReturnDefault] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 05 ListMode.TolerantCreateDefaultEntry] E X C E P T I O N
ThrowHard: RemoveAt | IndexOutOfRangeException

[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1000
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[
  1000
]

[Lut 00 ListMode.Normal] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1000
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": 0,
  ""Cancel"": false
}

[Lut 00 ListMode.Normal] C H A N G E D    L I S T
[]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1000
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1000
]

[Lut 01 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1000
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": 0,
  ""Cancel"": false
}

[Lut 01 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1000
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1000
]

[Lut 02 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1000
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": 0,
  ""Cancel"": false
}

[Lut 02 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1000
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[
  1000
]

[Lut 03 ListMode.Normal] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1000
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": 0,
  ""Cancel"": false
}

[Lut 03 ListMode.Normal] C H A N G E D    L I S T
[]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1000
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[
  1000
]

[Lut 04 ListMode.TolerantReturnDefault] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1000
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": 0,
  ""Cancel"": false
}

[Lut 04 ListMode.TolerantReturnDefault] C H A N G E D    L I S T
[]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 0,
  ""NewItems"": [
    1000
  ],
  ""OldItems"": null,
  ""NewStartingIndex"": 0,
  ""OldStartingIndex"": -1,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[
  1000
]

[Lut 05 ListMode.TolerantCreateDefaultEntry] E P R E
{
  ""Action"": 1,
  ""NewItems"": null,
  ""OldItems"": [
    1000
  ],
  ""NewStartingIndex"": -1,
  ""OldStartingIndex"": 0,
  ""Cancel"": false
}

[Lut 05 ListMode.TolerantCreateDefaultEntry] C H A N G E D    L I S T
[]
";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.RemoveAt."
                );

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.RemoveAt."
                );

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting builder content to match after IList.RemoveAt."
                );
            }
            #endregion
        }

        [TestMethod]
        public void Test_ModalContextState()
        {
            string actual, expected;
            bool controlKey = false, shiftKey = false;
            List<string> builder = new();

            var opc = new ObservablePreviewCollection<ItemCardModel>();

            var sc = opc.TrackContexts[nameof(ItemCardModel.Selection)];

            Assert.AreEqual(TrackMode.Single, sc.TrackMode);
            Assert.AreEqual(nameof(ItemCardModel.Selection), sc.PropertyInfo.Name);

            #region L o c a l F x				
            using var local = sc.WithOnDispose(
                onInit: (sender, e) =>
                {
                    sc.ModifiersRequest += localOnModifiersRequest;
                },
                onDispose: (sender, e) =>
                {
                    sc.ModifiersRequest -= localOnModifiersRequest;
                });

            void localOnModifiersRequest(object? sender, ModifiersRequestEventArgs e)
            {
                // [Careful] use local KeyBuilder thoroughout not 'builder'.
                var keyBuilder = new List<string>();
                if (controlKey)
                {
                    keyBuilder.Add("Control");
                }
                if (shiftKey)
                {
                    keyBuilder.Add("Shift");
                }
                e.Modifiers = keyBuilder.ToArray();
            }
            #endregion L o c a l F x
            var noDuplicate10 =
                this
                .PopulateDemoItems()
                .Cast<ItemCardModel>()
                .ToArray();
            opc.AddRange(
                [
                    noDuplicate10[0],
                    noDuplicate10[1],
                    noDuplicate10[2],
                ]);

            actual =
                JsonConvert
                .SerializeObject(opc, Formatting.Indented)
                .ParseReplace(new("Selection", typeof(ItemSelection)));

            actual.ToClipboardExpected();
            { }
            expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""None"",
    ""IsChecked"": true,
    ""Id"": ""0"",
    ""Description"": ""Apple"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""sweet\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""sweet\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""0"",
    ""QueryTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""FilterTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Apple\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""sweet\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""None"",
    ""IsChecked"": false,
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""None"",
    ""IsChecked"": false,
    ""Id"": ""2"",
    ""Description"": ""Carrot"",
    ""Keywords"": ""[\""vegetable\"", \""orange\"", \""root\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""orange\"", \""root\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""2"",
    ""QueryTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""FilterTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Carrot\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""orange\\\"", \\\""root\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  }
]"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 3 items"
            );
            { }

            subtest_SelectExclusiveCarrot();
            subtest_SelectExclusiveBanana();
            subtest_SelectSameExclusiveToToggle();
            subtest_SelectExclusiveApple();
            subtest_SelectPrimaryBanana();
            subtest_SelectPrimaryCarrot();
            subtest_SelectMultiToPrimaryApple();
            subtest_SelectPrimaryToNoneApple();
#if false
            subtest_SelectionContext9();
            subtest_SelectionContext10();
#endif

            #region S U B T E S T S
            void subtest_SelectExclusiveCarrot()
            {
                localTapItem(2);

                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems, Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Exclusive"",
    ""IsChecked"": false,
    ""Id"": ""2"",
    ""Description"": ""Carrot"",
    ""Keywords"": ""[\""vegetable\"", \""orange\"", \""root\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""orange\"", \""root\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""2"",
    ""QueryTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""FilterTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Carrot\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""orange\\\"", \\\""root\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting one selected item @ Carrot."
                );
            }

            void subtest_SelectExclusiveBanana()
            {
                localTapItem(1);

                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems, Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Exclusive"",
    ""IsChecked"": false,
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting one selected item @ changed to Banana."
                );
            }

            void subtest_SelectSameExclusiveToToggle()
            {
                localTapItem(1);


                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems, Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));
                actual.ToClipboardExpected();
                { }
                expected = @" 
[]";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting no selections. The exclusive should toggle."
                );
            }

            void subtest_SelectExclusiveApple()
            {
                localTapItem(0);

                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems, Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Exclusive"",
    ""IsChecked"": true,
    ""Id"": ""0"",
    ""Description"": ""Apple"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""sweet\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""sweet\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""0"",
    ""QueryTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""FilterTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Apple\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""sweet\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting one selected item @ Apple."
                );
            }

            void subtest_SelectPrimaryBanana()
            {
                localTapItem(1, control: true);

                // [Careful] This result is not supposed to be ordered
                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems.OrderBy(_ => _.Id), Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));
                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": true,
    ""Id"": ""0"",
    ""Description"": ""Apple"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""sweet\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""sweet\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""0"",
    ""QueryTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""FilterTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Apple\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""sweet\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Primary"",
    ""IsChecked"": false,
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting 2 items with Banana primary."
                );
            }

            void subtest_SelectPrimaryCarrot()
            {
                localTapItem(2, control: true);
                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems, Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));

                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": true,
    ""Id"": ""0"",
    ""Description"": ""Apple"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""sweet\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""sweet\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""0"",
    ""QueryTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""FilterTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Apple\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""sweet\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": false,
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Primary"",
    ""IsChecked"": false,
    ""Id"": ""2"",
    ""Description"": ""Carrot"",
    ""Keywords"": ""[\""vegetable\"", \""orange\"", \""root\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""orange\"", \""root\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""2"",
    ""QueryTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""FilterTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Carrot\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""orange\\\"", \\\""root\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting 3 items with Carrot primary"
                );
            }

            void subtest_SelectMultiToPrimaryApple()
            {
                localTapItem(0, control: true);
                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems.OrderBy(_ => _.Id), Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));

                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Primary"",
    ""IsChecked"": true,
    ""Id"": ""0"",
    ""Description"": ""Apple"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""sweet\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""sweet\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""0"",
    ""QueryTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""FilterTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Apple\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""sweet\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": false,
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": false,
    ""Id"": ""2"",
    ""Description"": ""Carrot"",
    ""Keywords"": ""[\""vegetable\"", \""orange\"", \""root\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""orange\"", \""root\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""2"",
    ""QueryTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""FilterTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Carrot\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""orange\\\"", \\\""root\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting 3 items with Apple primary"
                );
            }

            void subtest_SelectPrimaryToNoneApple()
            {
                localTapItem(0, control: true);
                actual =
                    JsonConvert
                    .SerializeObject(sc.CurrentItems, Formatting.Indented)
                    .ParseReplace(new("Selection", typeof(ItemSelection)));

                actual.ToClipboardExpected();
                { }
                expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": false,
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": ""Multi"",
    ""IsChecked"": false,
    ""Id"": ""2"",
    ""Description"": ""Carrot"",
    ""Keywords"": ""[\""vegetable\"", \""orange\"", \""root\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""orange\"", \""root\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""2"",
    ""QueryTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""FilterTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Carrot\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""orange\\\"", \\\""root\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  }
]"
                ;

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting 2 multi items with Apple removed"
                );
            }
            void subtest_SelectionContext9()
            {
            }
            void subtest_SelectionContext10()
            {
            }

            void localTapItem(int index, bool control = false, bool shift = false)
            {
                try
                {
                    controlKey = control;
                    shiftKey = shift;
                    sc.ItemPress(opc[index]);
                    Assert.ReferenceEquals(opc[index], sc.PressedItem);
                    sc.ItemRelease(opc[index]);
                }
                finally
                {
                    controlKey = shiftKey = false;
                }
            }
            #endregion S U B T E S T S
        }

        [TestMethod]
        public void Test_ModalContextBoolean()
        {
            string actual, expected;
            bool controlKey = false, shiftKey = false;
            List<string> builder = new();

            var opc = new ObservablePreviewCollection<ItemCardModel>();

            var sc = opc.TrackContexts[nameof(ItemCardModel.IsChecked)];

            Assert.AreEqual(TrackMode.Multiple, sc.TrackMode);
            Assert.AreEqual(nameof(ItemCardModel.IsChecked), sc.PropertyInfo.Name);
            { }

            var noDuplicate10 =
                this
                .PopulateDemoItems()
                .Cast<ItemCardModel>()
                .ToArray();
            opc.AddRange(
                [
                    noDuplicate10[0],
                    noDuplicate10[1],
                    noDuplicate10[2],
                ]);
        }
    }
    namespace OPC
    {
        enum TestPredicate
        {
            [Where("IsSelected", WherePredicate.IsNotZero)]
            IsSelected,
        }
        public class OPCItem : INotifyPropertyChanged
        {
            internal static uint AutoIdCount { get; set; } = 0;
            [PrimaryKey]
            public string Id { get; init; } = $"{AutoIdCount++}";

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (!Equals(_isSelected, value))
                    {
                        _isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }
            bool _isSelected = false;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
