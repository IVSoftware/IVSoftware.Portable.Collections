using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.MSTest.TestTargets;
using IVSoftware.Portable.Collections.MSTest.TestUtils;
using System.Diagnostics;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_Follow
{
    [TestMethod]
    public async Task Test_Follow101()
    {
        Debug.WriteLine(SynchronizationContext.Current?.GetType().FullName ?? "null");
        SynchronizationContext.SetSynchronizationContext(null);
        Debug.WriteLine(SynchronizationContext.Current?.GetType().FullName ?? "null");

        var opc = new ObservablePreviewCollection<ItemCardModel>();
        opc.PopulateDemoItems();

        var fcs= opc.FollowContexts[nameof(ItemCardModel.Selection)];
        var fcc = opc.FollowContexts[nameof(ItemCardModel.IsChecked)];

        Assert.AreEqual(10, opc.Count, $"Expecting Count to reflect full count.");
        await opc.ActivateFilters(StdPredicate.IsChecked);

        Assert.AreEqual(4, opc.Count, $"Expecting Count to reflect filtered count.");
        Assert.AreEqual(10, opc.CountUnfiltered, $"Expecting Count to reflect filtered count.");
        Assert.IsTrue(opc.IsFiltering);
        { }
        Assert.IsNotNull(opc.MarkdownContext);
        Assert.IsTrue(opc.MarkdownContext.QueryFilterConfig.HasFlag(SQLiteMarkdown.QueryFilterConfig.Filter));
        opc.MarkdownContext.QueryFilterConfig = SQLiteMarkdown.QueryFilterConfig.Filter;
        { }
    }
}
