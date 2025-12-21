using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.WinOS.MSTest.Extensions.STA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_ReadMeClaims
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

    [TestMethod]
    public void Test_ReflectionCachingWhenFound()
    {
        // Evaluate reflection on an unknown type;
        object unk = new Microsoft.Maui.Controls.Button();

        var cache = new TolerantDictionary<string, PropertyInfo>();

        cache.CollectionChanging += OnCollectionChanging;

        // Local function expresses familiar handler 'shape'.
        void OnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
        {
            if (sender is IDictionary && sender is ITolerant)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangingAction.Replace:
                        if (e.GetNewItemSingle() is DictionaryEntryPreview entry &&
                            entry.Key is string key &&
                            entry.Value is null)
                        {
                            entry.Value = unk.GetType().GetProperty(key);
                        }
                        break;
                }
            }
        }

        // TEST BEGINS HERE
        PropertyInfo? pi = cache["IsVisible"];
        Assert.IsTrue(pi is PropertyInfo, "Expecting an instance of PropertyInfo.");
        Assert.IsTrue(cache.ContainsKey("IsVisible"), "Expecting O(1) retrieval in future calls.");
    }

    [TestMethod]
    public void Test_ReflectionCachingWhenNotFound()
    {
        object unk = new Microsoft.Maui.Controls.Button();
        int probeCount = 0;

        var cache = new TolerantDictionary<string, PropertyInfo>();
        cache.CollectionChanging += OnCollectionChanging;

        void OnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
        {
            if (sender is IDictionary && sender is ITolerant)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangingAction.Replace:
                        if (e.GetNewItemSingle() is DictionaryEntryPreview entry &&
                            entry.Key is string key &&
                            entry.Value is null)
                        {
                            var pi = unk.GetType().GetProperty(key);
                            if (pi is null)
                            {
                                entry.Value = TolerantValue.ExplicitNull;
                            }
                            else
                            {
                                entry.Value = pi;
                            }
                            probeCount++;
                        }
                        break;
                }
            }
        }

        // TEST BEGINS HERE
        // Premise: The WinForms version of "IsVisible" is *not* supported on the MAUI Button.
        PropertyInfo? pi = cache["Visible"];

        Assert.IsTrue(pi is null, "Expecting PropertyInfo not found.");
        Assert.IsTrue(cache.ContainsKey("Visible"), "Expecting O(1) retrieval in future calls.");
        Assert.IsTrue(probeCount == 1, "Expecting probeCount is incremented by event handler.");

        // VERIFY THAT NULL RETRIEVAL IS O(1)
        pi = cache["Visible"];
        Assert.IsTrue(probeCount == 1, "Expecting probeCount is still 1 because key is found.");
    }

    [TestMethod]
    public void Test_BriskMultiKey()
    {
        object unk = new Microsoft.Maui.Controls.Button();

        var brisk = new BriskDictionary();
        IDictionary dunk = brisk[typeof(PropertyInfo)];
        dunk["IsVisible"] = unk.GetType().GetProperty("IsVisible");

        if (dunk["IsVisible"] is PropertyInfo pi)
        {
            pi.SetValue(unk, true);
        }

        brisk.Clear();

        dunk = brisk[unk.GetType(), typeof(PropertyInfo)];

        Assert.IsInstanceOfType<TolerantDictionary<object, object>>(dunk);
    }

    [TestMethod]
    public void Test_BriskTypeSafety()
    {
        object unk = new Microsoft.Maui.Controls.Button();

        var brisk = new BriskDictionary();

        // Initialize an arbitrary unknown type for reflection.
        _ = brisk[unk.GetType(), typeof(ConstructorInfo)].AsStronglyTypedDictionary<string, ConstructorInfo>();
        _ = brisk[unk.GetType(), typeof(PropertyInfo)].AsStronglyTypedDictionary<string, PropertyInfo>();
        _ = brisk[unk.GetType(), typeof(MethodInfo)].AsStronglyTypedDictionary<string, MethodInfo>();
        _ = brisk[unk.GetType(), typeof(EventInfo)].AsStronglyTypedDictionary<string, EventInfo>();


        var dunk = brisk[unk.GetType(), typeof(PropertyInfo)];

        Assert.IsInstanceOfType<TolerantDictionary<string, PropertyInfo>>(dunk);
    }

    [TestMethod]
    public void Test_ExplicitMode()
    {
        object unk = new Microsoft.Maui.Controls.Button();

        var brisk = new BriskDictionary();

        var dunk =
            brisk[unk.GetType(), typeof(PropertyInfo)]
            .AsStronglyTypedDictionary<string, PropertyInfo>(mode: DictionaryMode.InsistentNotNull)
            .WithCollectionChangedEvent(onCollectionChanged: (sender, e) =>
            { 

            });

        Assert.IsInstanceOfType<InsistentDictionary<string, PropertyInfo>>(dunk);
    }

    [TestMethod]
    public void Test_WhenToHandle()
    {
        object unk = new Microsoft.Maui.Controls.Button();

        var brisk = new BriskDictionary();

        // One-Time init
        _ =
           brisk[unk.GetType(), typeof(PropertyInfo)]
           .AsStronglyTypedDictionary<string, PropertyInfo>()
           .WithCollectionChangingEvent(onCollectionChanging: (sender, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangingAction.Replace:
                        if (e.GetNewItemSingle() is DictionaryEntryPreview entry && entry.Value is null)
                        {
                            // If sender is IDictionary then we can query its Brisk key (if it has one).
                            if (sender is IDictionary dict)
                            {
                                if (dict.Ancestors().OfType<Type>().FirstOrDefault() is { } parentType)
                                {
                                    Assert.AreEqual(typeof(Microsoft.Maui.Controls.Button), parentType);
                                    { }
                                }
                            }
                        }
                        break;
                }
            });

        // On-demand reflection
        if(brisk[unk.GetType(), typeof(PropertyInfo)]["IsVisible"] is PropertyInfo pi)
        {
            pi.SetValue(unk, true);
        }
        else
        {
            this.ThrowSoft<NotSupportedException>($"{unk.GetType().Name} does not support the 'IsVisible' property");
        }
    }
}
