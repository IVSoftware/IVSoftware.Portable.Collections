using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.MSTest.TestTargets;
using IVSoftware.Portable.Collections.MSTest.TestUtils;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.Threading;
using IVSoftware.WinOS.MSTest.Extensions;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_Track
{
    [TestInitialize]
    public void TestInitialize()
    {
        Debug.Assert(SynchronizationContext.Current?.GetType().FullName is null);
    }

    [TestMethod]
    public async Task Test_Track101a()
    {
        Debug.WriteLine(SynchronizationContext.Current?.GetType().FullName ?? "null");
        SynchronizationContext.SetSynchronizationContext(null);
        Debug.WriteLine(SynchronizationContext.Current?.GetType().FullName ?? "null");

        var opc = new ObservablePreviewCollection<ItemCardModel>();
        opc.PopulateDemoItems();

        var fcs= opc.TrackContexts[nameof(ItemCardModel.Selection)];
        var fcc = opc.TrackContexts[nameof(ItemCardModel.IsChecked)];

        Assert.AreEqual(10, opc.Count, $"Expecting Count to reflect full count.");
        opc.ActivateFilters(StdPredicate.IsChecked);
        await opc;
        { }

        Assert.AreEqual(4, opc.Count, $"Expecting Count to reflect filtered count.");
        Assert.AreEqual(10, opc.CountUnfiltered, $"Expecting Count to reflect filtered count.");
        Assert.IsTrue(opc.IsFiltering);
        { }
        Assert.IsNotNull(opc.MarkdownContext);
        Assert.IsTrue(opc.MarkdownContext.QueryFilterConfig.HasFlag(QueryFilterConfig.Filter));
        opc.MarkdownContext.QueryFilterConfig = QueryFilterConfig.Filter;
        { }
    }

    [TestMethod]
    public async Task Test_Track101()
    {
        string actual, expected;
        List<string> builder = new();
        int reconcileCount = 0;
        #region L o c a l F x				
        using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                AwaitedEventArgs.Awaited += localOnAwaited;
            },
            onDispose: (sender, e) =>
            {
                AwaitedEventArgs.Awaited -= localOnAwaited;
            });
        void localOnAwaited(object? sender, AwaitedEventArgs e)
        {
            switch (e.Caller)
            {
                case "ReconcileFilters":
                    reconcileCount++;
                    break;
                default:
                    break;
            }
        }
        void ResetReconcileCount() => reconcileCount = 0;
        #endregion L o c a l F x

        var opc = new ObservablePreviewCollection<ItemCardModel>();
        Assert.IsNotNull(opc.MarkdownContext);
        opc.MarkdownContext.QueryFilterConfig = QueryFilterConfig.Filter;
        var fcs = opc.TrackContexts[nameof(ItemCardModel.Selection)]!;
        var fcc = opc.TrackContexts[nameof(ItemCardModel.IsChecked)]!;        

        opc.PopulateDemoItems();

        await subtest_IsFilteringBasedOnMarkdown();
        await subtest_CheckBoxFiltering();
        await subtest_ComboFiltering();
        subtest_TrackWhereBriskDict();
        subtest_OnAnyActivateTracking();
        subtest_OnAnyActivateWhere();
        subtest_OnCombineTrackAndWhere();
        subtest_OnLoadTrackContexts();
        subtest_AddRemoveVisibleItem();
        subtest_Track10110();

        #region S U B T E S T S
        // Filter list by MD only
        async Task subtest_IsFilteringBasedOnMarkdown()
        {
            ResetReconcileCount();
            Assert.AreEqual(
                QueryFilterConfig.Filter,
                opc.MarkdownContext.QueryFilterConfig,
                $"Expecting mode is Filter thoughout this Test.");
            Assert.AreEqual(
                SearchEntryState.Cleared,
                opc.MarkdownContext.SearchEntryState,
                $"Expecting return to Cleared (because config is Filter.");
            Assert.AreEqual(
                FilteringState.Armed, 
                opc.MarkdownContext.FilteringState,
                $"Expecting return to Armed (because state is Cleared.");
            Assert.IsFalse(
                opc.IsFiltering,
                $"Expecting false, because no input has occurred");
            Assert.AreEqual(0, opc.UnfilteredItems.Count);

            // Transition: not filtering -> filtering
            // Enter a single character.
            // 1. This should not affect query state at all (it stays cleared).
            // 2. The filtering state should change to Active.
            // 3. The IsFiltering bool should go True.
            opc.MarkdownContext.InputText = "A";
            Assert.AreEqual(
                SearchEntryState.Cleared,
                opc.MarkdownContext.SearchEntryState,
                $"Expecting return to Cleared (because config is Filter.");
            Assert.AreEqual(
                FilteringState.Active,
                opc.MarkdownContext.FilteringState);
            Assert.IsTrue(opc.IsFiltering);
            Assert.AreEqual(10, opc.UnfilteredItems.Count);
            await opc;
            { }
            // In this case, all match
            Assert.AreEqual(10, opc.Count);
            Assert.AreEqual(0, reconcileCount, "Expecting list is unaltered by this rf.");
            { }

            // Transition: filtering -> not filtering
            // Backspace
            // 1. This should not affect query state at all (it stays cleared).
            // 2. The filtering state should change to Active.
            // 3. The IsFiltering bool should go True.

            ResetReconcileCount(); 
            opc.MarkdownContext.Clear(all: true);
            await opc;
            { }
            Assert.AreEqual(
                SearchEntryState.Cleared,
                opc.MarkdownContext.SearchEntryState,
                $"Expecting return to Cleared (because config is Filter.");
            Assert.AreEqual(
                FilteringState.Armed,
                opc.MarkdownContext.FilteringState);
            Assert.IsFalse(opc.IsFiltering);
            Assert.AreEqual(0, opc.UnfilteredItems.Count);
            { }

            opc.MarkdownContext.InputText = "fruit";
            await opc;
            { }
            Assert.AreEqual(1, reconcileCount, "Expecting single reconcile.");
            { }

            actual = JsonConvert.SerializeObject(opc, Formatting.Indented);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
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
    ""Selection"": 0,
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
    ""Selection"": 0,
    ""IsChecked"": false,
    ""Id"": ""4"",
    ""Description"": ""Strawberry"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""berry\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""berry\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""4"",
    ""QueryTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""FilterTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Strawberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""berry\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": true,
    ""Id"": ""6"",
    ""Description"": ""Orange"",
    ""Keywords"": ""[\""fruit\"", \""citrus\"", \""orange\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""citrus\"", \""orange\"""",
    ""Tags"": ""[fruit][produce][citrus]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""6"",
    ""QueryTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""FilterTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""TagMatchTerm"": ""[fruit][produce][citrus]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Orange\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""citrus\\\"", \\\""orange\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][citrus]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": false,
    ""Id"": ""7"",
    ""Description"": ""Tomato"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""savory\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""savory\"""",
    ""Tags"": ""[fruit][vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""7"",
    ""QueryTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""FilterTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""TagMatchTerm"": ""[fruit][vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Tomato\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""savory\\\""]\"",\r\n  \""Tags\"": \""[fruit][vegetable][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": true,
    ""Id"": ""9"",
    ""Description"": ""Blueberry"",
    ""Keywords"": ""[\""fruit\"", \""blue\"", \""small\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""blue\"", \""small\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""9"",
    ""QueryTerm"": ""blueberry~fruit~blue~small~[fruit][produce][berry]"",
    ""FilterTerm"": ""blueberry~fruit~blue~small~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Blueberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""blue\\\"", \\\""small\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  }
]";
            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting fruits"
            );

            opc.MarkdownContext.Clear();
            Assert.AreEqual(
                10,
                opc.Count,
                "Expecting fruits"
            );
        }

        // Filter list by Where only
        async Task subtest_CheckBoxFiltering()
        {
            ResetReconcileCount();
            opc.ActivateFilters(StdPredicate.IsChecked);
            await opc;
            { }
            Assert.AreEqual(1, reconcileCount);
            Assert.AreEqual(4, opc.Count, "Expecting 4 checked items visible.");
            using(opc.BeginFilterAtom())
            {
                opc.DeactivateFilters(StdPredicate.IsChecked);
                opc.ActivateFilters(StdPredicate.IsUnchecked);
            }
            await opc;
            { }
            Assert.AreEqual(6, opc.Count, "Expecting 6 unchecked items visible.");

            opc.ClearFilters();
            await opc;
            { }
            Assert.AreEqual(10, opc.Count, "Expecting all visible.");
        }

        async Task subtest_ComboFiltering()
        {
            ResetReconcileCount();
            opc.ClearFilters();
            using (opc.BeginFilterAtom())
            {
                opc.ActivateFilters(StdPredicate.IsChecked);
                opc.MarkdownContext.InputText = "fruit";
            }
            await opc;
            { }

            Assert.AreEqual(1, reconcileCount);
            actual = JsonConvert.SerializeObject(opc, Formatting.Indented);
            actual.ToClipboardExpected();
            { }

            expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
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
    ""Selection"": 0,
    ""IsChecked"": true,
    ""Id"": ""6"",
    ""Description"": ""Orange"",
    ""Keywords"": ""[\""fruit\"", \""citrus\"", \""orange\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""citrus\"", \""orange\"""",
    ""Tags"": ""[fruit][produce][citrus]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""6"",
    ""QueryTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""FilterTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""TagMatchTerm"": ""[fruit][produce][citrus]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Orange\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""citrus\\\"", \\\""orange\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][citrus]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": true,
    ""Id"": ""9"",
    ""Description"": ""Blueberry"",
    ""Keywords"": ""[\""fruit\"", \""blue\"", \""small\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""blue\"", \""small\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""9"",
    ""QueryTerm"": ""blueberry~fruit~blue~small~[fruit][produce][berry]"",
    ""FilterTerm"": ""blueberry~fruit~blue~small~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Blueberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""blue\\\"", \\\""small\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  }
]";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting CHECKED fruit matches only"
            );

            using (opc.BeginFilterAtom())
            {
                opc.DeactivateFilters(StdPredicate.IsChecked);
                opc.ActivateFilters(StdPredicate.IsUnchecked);
                opc.MarkdownContext.InputText = "fruit";
            }
            await opc;
            { }

            actual = JsonConvert.SerializeObject(opc, Formatting.Indented);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
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
    ""Selection"": 0,
    ""IsChecked"": false,
    ""Id"": ""4"",
    ""Description"": ""Strawberry"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""berry\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""berry\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""4"",
    ""QueryTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""FilterTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Strawberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""berry\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": false,
    ""Id"": ""7"",
    ""Description"": ""Tomato"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""savory\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""savory\"""",
    ""Tags"": ""[fruit][vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""7"",
    ""QueryTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""FilterTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""TagMatchTerm"": ""[fruit][vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Tomato\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""savory\\\""]\"",\r\n  \""Tags\"": \""[fruit][vegetable][produce]\""\r\n}""
  }
]";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting UNCHECKED fruit matches only"
            );

            opc.ClearFilters(clearInputText: false);
            await opc;
            { }

            actual = JsonConvert.SerializeObject(opc, Formatting.Indented);
            actual.ToClipboardExpected();
            { }
            expected = @" 
[
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
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
    ""Selection"": 0,
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
    ""Selection"": 0,
    ""IsChecked"": false,
    ""Id"": ""4"",
    ""Description"": ""Strawberry"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""berry\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""berry\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""4"",
    ""QueryTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""FilterTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Strawberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""berry\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": true,
    ""Id"": ""6"",
    ""Description"": ""Orange"",
    ""Keywords"": ""[\""fruit\"", \""citrus\"", \""orange\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""citrus\"", \""orange\"""",
    ""Tags"": ""[fruit][produce][citrus]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""6"",
    ""QueryTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""FilterTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""TagMatchTerm"": ""[fruit][produce][citrus]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Orange\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""citrus\\\"", \\\""orange\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][citrus]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": false,
    ""Id"": ""7"",
    ""Description"": ""Tomato"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""savory\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""savory\"""",
    ""Tags"": ""[fruit][vegetable][produce]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""7"",
    ""QueryTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""FilterTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""TagMatchTerm"": ""[fruit][vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Tomato\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""savory\\\""]\"",\r\n  \""Tags\"": \""[fruit][vegetable][produce]\""\r\n}""
  },
  {
    ""ShowCheckboxes"": true,
    ""Selection"": 0,
    ""IsChecked"": true,
    ""Id"": ""9"",
    ""Description"": ""Blueberry"",
    ""Keywords"": ""[\""fruit\"", \""blue\"", \""small\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""blue\"", \""small\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsEditing"": false,
    ""PrimaryKey"": ""9"",
    ""QueryTerm"": ""blueberry~fruit~blue~small~[fruit][produce][berry]"",
    ""FilterTerm"": ""blueberry~fruit~blue~small~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Blueberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""blue\\\"", \\\""small\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  }
]";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting ALL FRUITS (6)."
            );
        }

        void subtest_TrackWhereBriskDict()
        {
        }
        void subtest_OnAnyActivateTracking()
        {
        }

        void subtest_OnAnyActivateWhere()
        {
        }

        void subtest_OnCombineTrackAndWhere()
        {
        }

        void subtest_OnLoadTrackContexts()
        {
            _ = fcc.CurrentItems.Length;
            _ = fcc.CurrentItemsB.Length;
            { }
        }
        void subtest_AddRemoveVisibleItem()
        {
        }
        void subtest_Track10110()
        {
        }
        #endregion S U B T E S T S
    }
}
