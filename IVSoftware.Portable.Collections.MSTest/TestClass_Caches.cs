using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.MSTest.TestTargets;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.WinOS.MSTest.Extensions;
using IVSoftware.WinOS.MSTest.Extensions.STA;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static IVSoftware.Portable.Collections.Framework;
using static IVSoftware.Portable.Threading.Extensions;
using Button = System.Windows.Forms.Button;
using Color = System.Drawing.Color;
using DictionaryEntry = IVSoftware.Portable.Collections.Dictionaries.DictionaryEntryPreview;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_Caches
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
    public void Test_ManualCaches()
    {
        string actual, expected;
        List<string> builder = new();
        IDictionary? dunk;
        XElement xunk;

        using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
            },
            onDispose: (sender, e) =>
            {
                Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
            });
        void localOnBeginThrowOrAdvise(object? sender, Throw e)
        {
            builder.Add(e.Message);
            e.Handled = true;
        }
        ;

        subtestCacheProperties();

        #region S U B T E S T S 
        void subtestCacheProperties()
        {
            using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                BriskReset();

                actual = Brisk.Model.ToString();
                actual.ToClipboardExpected();
                { }
                expected = @" 
<model />";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting model is empty."
                );
            },
            onDispose: (sender, e) =>
            {
                BriskReset();
            });

            foreach (var type in new[] { typeof(EmployeeRecord), typeof(SalaryRecord) })
            {
                Framework
                    .Brisk[type, typeof(ConstructorInfo)]
                    .AddRange(
                        type
                        .GetConstructors()
                        .Select(_ => new DictionaryEntry(_.Name, _))
                        .WithDuplicateNamesIndexed());

                Framework
                    .Brisk[type, typeof(MethodInfo)]
                    .AddRange(
                        type
                        .GetDeclaredUserMethods()
                        .Select(_ => new DictionaryEntry(_.Name, _))
                        .WithDuplicateNamesIndexed());

                Framework
                    .Brisk[type, typeof(PropertyInfo)]
                    .AddRange(type.GetProperties().Select(_ => new DictionaryEntry(_.Name, _)));

                Framework
                    .Brisk[type, typeof(EventInfo)]
                    .AddRange(type.GetEvents().Select(_ => new DictionaryEntry(_.Name, _)));
            }
            var model = Brisk.ViewExpandedModel();
            actual = model;
            actual.ToClipboardExpected();
            { }
            expected = @" 
<model>
  <xnode text=""EmployeeRecord"">
    <xnode text=""ConstructorInfo"" dunk=""[System.Object🡒Object] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#00</key>
          <value>Void .ctor()</value>
        </entry>
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#01</key>
          <value>Void .ctor(Int32, System.String)</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""MethodInfo"" dunk=""[System.Object🡒Object] Count=05"">
      <values count=""5"">
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#00</key>
          <value>Void Promote(System.String)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#01</key>
          <value>Void Promote(System.String, System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#02</key>
          <value>Void Promote(System.String, System.String, System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Terminate</key>
          <value>Void Terminate()</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ToString</key>
          <value>System.String ToString()</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""PropertyInfo"" dunk=""[System.Object🡒Object] Count=07"">
      <values count=""7"">
        <entry type=""RuntimePropertyInfo"">
          <key>Id</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Name</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Department</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Title</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Salary</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>HireDate</key>
          <value>DateTime</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>IsActive</key>
          <value>Boolean</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""EventInfo"" dunk=""[System.Object🡒Object] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeEventInfo"">
          <key>Promoted</key>
          <value>System.EventHandler Promoted</value>
        </entry>
        <entry type=""RuntimeEventInfo"">
          <key>Terminated</key>
          <value>System.EventHandler Terminated</value>
        </entry>
      </values>
    </xnode>
  </xnode>
  <xnode text=""SalaryRecord"">
    <xnode text=""ConstructorInfo"" dunk=""[System.Object🡒Object] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#00</key>
          <value>Void .ctor()</value>
        </entry>
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#01</key>
          <value>Void .ctor(Int32, System.Decimal)</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""MethodInfo"" dunk=""[System.Object🡒Object] Count=04"">
      <values count=""4"">
        <entry type=""RuntimeMethodInfo"">
          <key>ApplyRaise</key>
          <value>Void ApplyRaise(System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ApplyBonus</key>
          <value>Void ApplyBonus(System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>TotalCompensation</key>
          <value>System.Decimal TotalCompensation()</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ToString</key>
          <value>System.String ToString()</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""PropertyInfo"" dunk=""[System.Object🡒Object] Count=05"">
      <values count=""5"">
        <entry type=""RuntimePropertyInfo"">
          <key>EmployeeId</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>BaseSalary</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Bonus</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>EffectiveDate</key>
          <value>DateTime</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Currency</key>
          <value>String</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""EventInfo"" dunk=""[System.Object🡒Object] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeEventInfo"">
          <key>SalaryChanged</key>
          <value>System.EventHandler SalaryChanged</value>
        </entry>
        <entry type=""RuntimeEventInfo"">
          <key>BonusApplied</key>
          <value>System.EventHandler BonusApplied</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>"
            ;

            IDictionary dunk;
            foreach (var type in new[] { typeof(EmployeeRecord), typeof(SalaryRecord) })
            {
                dunk = Framework
                      .Brisk[type, typeof(ConstructorInfo)]
                      .AsStronglyTypedDictionary<string, ConstructorInfo>();

                dunk = Framework
                      .Brisk[type, typeof(MethodInfo)]
                      .AsStronglyTypedDictionary<string, MethodInfo>();

                dunk = Framework
                      .Brisk[type, typeof(PropertyInfo)]
                      .AsStronglyTypedDictionary<string, PropertyInfo>();

                dunk = Framework
                      .Brisk[type, typeof(EventInfo)]
                      .AsStronglyTypedDictionary<string, EventInfo>();
            }


            model = Brisk.ViewExpandedModel();

            actual = model;
            actual.ToClipboardExpected();
            { }
            expected = @" 
<model>
  <xnode text=""EmployeeRecord"" key=""[KeyObject]"">
    <xnode text=""ConstructorInfo"" key=""[KeyObject]"" dunk=""[String🡒ConstructorInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#00</key>
          <value>Void .ctor()</value>
        </entry>
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#01</key>
          <value>Void .ctor(Int32, System.String)</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""MethodInfo"" key=""[KeyObject]"" dunk=""[String🡒MethodInfo] Count=05"">
      <values count=""5"">
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#00</key>
          <value>Void Promote(System.String)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#01</key>
          <value>Void Promote(System.String, System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#02</key>
          <value>Void Promote(System.String, System.String, System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Terminate</key>
          <value>Void Terminate()</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ToString</key>
          <value>System.String ToString()</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=07"">
      <values count=""7"">
        <entry type=""RuntimePropertyInfo"">
          <key>Id</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Name</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Department</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Title</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Salary</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>HireDate</key>
          <value>DateTime</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>IsActive</key>
          <value>Boolean</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""EventInfo"" key=""[KeyObject]"" dunk=""[String🡒EventInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeEventInfo"">
          <key>Promoted</key>
          <value>System.EventHandler Promoted</value>
        </entry>
        <entry type=""RuntimeEventInfo"">
          <key>Terminated</key>
          <value>System.EventHandler Terminated</value>
        </entry>
      </values>
    </xnode>
  </xnode>
  <xnode text=""SalaryRecord"" key=""[KeyObject]"">
    <xnode text=""ConstructorInfo"" key=""[KeyObject]"" dunk=""[String🡒ConstructorInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#00</key>
          <value>Void .ctor()</value>
        </entry>
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#01</key>
          <value>Void .ctor(Int32, System.Decimal)</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""MethodInfo"" key=""[KeyObject]"" dunk=""[String🡒MethodInfo] Count=04"">
      <values count=""4"">
        <entry type=""RuntimeMethodInfo"">
          <key>ApplyRaise</key>
          <value>Void ApplyRaise(System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ApplyBonus</key>
          <value>Void ApplyBonus(System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>TotalCompensation</key>
          <value>System.Decimal TotalCompensation()</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ToString</key>
          <value>System.String ToString()</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=05"">
      <values count=""5"">
        <entry type=""RuntimePropertyInfo"">
          <key>EmployeeId</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>BaseSalary</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Bonus</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>EffectiveDate</key>
          <value>DateTime</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Currency</key>
          <value>String</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""EventInfo"" key=""[KeyObject]"" dunk=""[String🡒EventInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeEventInfo"">
          <key>SalaryChanged</key>
          <value>System.EventHandler SalaryChanged</value>
        </entry>
        <entry type=""RuntimeEventInfo"">
          <key>BonusApplied</key>
          <value>System.EventHandler BonusApplied</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting model to match."
            );

            dunk = Brisk[typeof(EmployeeRecord)];
            Assert.AreEqual(0, dunk.Count, $"Expecting this is 'not' the target. FYI");
            dunk = Brisk[typeof(EmployeeRecord), typeof(PropertyInfo)];
            Assert.AreNotEqual(0, dunk.Count, $"Expecting this 'is' the target. FYI");
            PropertyInfo? pi;

            pi = (PropertyInfo)Brisk[typeof(EmployeeRecord), typeof(PropertyInfo)]["Name"]!;
            Assert.IsNotNull(pi);
            Assert.IsInstanceOfType<PropertyInfo>(pi);

            xunk =
                Brisk[typeof(EmployeeRecord), typeof(PropertyInfo)]
                .ToXDunk(@throw: true)!;

        }
        #endregion S U B T E S T S
    }

    
    [TestMethod]
    public void Test_AsStronglyTypedDictionary ()
    {
        string actual, expected;
        List<string> builder = new();
        IObservableDictionary? dunk;
        XElement xunk;

        using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
            },
            onDispose: (sender, e) =>
            {
                Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
            });
        void localOnBeginThrowOrAdvise(object? sender, Throw e)
        {
            builder.Add(e.Message);
            e.Handled = true;
        };

        subtestCacheProperties();
        { }

        #region S U B T E S T S 

        void subtestCacheProperties()
        {
            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    BriskReset();
                },
                onDispose: (sender, e) =>
                {
                    BriskReset();
                });

            foreach (var type in new[] { typeof(EmployeeRecord), typeof(SalaryRecord) })
            {
                dunk = Framework
                    .Brisk[type, typeof(ConstructorInfo)]
                    .AsStronglyTypedDictionary<string, ConstructorInfo>(@throw: true)!;

                dunk.AddRange(
                        type
                        .GetConstructors()
                        .Select(_ => new DictionaryEntry(_.Name, _))
                        .WithDuplicateNamesIndexed());

                dunk = Framework
                    .Brisk[type, typeof(MethodInfo)]
                    .AsStronglyTypedDictionary<string, MethodInfo>(@throw: true)!;

                dunk.AddRange(
                        type
                        .GetDeclaredUserMethods()
                        .Select(_ => new DictionaryEntry(_.Name, _))
                        .WithDuplicateNamesIndexed());

                dunk = Framework
                    .Brisk[type, typeof(PropertyInfo)]
                    .AsStronglyTypedDictionary<string, PropertyInfo>(@throw: true)!;
                dunk.AddRange(type.GetProperties().Select(_ => new DictionaryEntry(_.Name, _)));

                dunk = Framework
                    .Brisk[type, typeof(EventInfo)]
                    .AsStronglyTypedDictionary<string, PropertyInfo>(@throw: true)!;

                dunk.AddRange(type.GetEvents().Select(_ => new DictionaryEntry(_.Name, _)));
            }
            var model = Brisk.ViewExpandedModel();
            { }

            actual = model;
            actual.ToClipboardExpected();
            { }
            expected = @" 
<model>
  <xnode text=""EmployeeRecord"" key=""[KeyObject]"">
    <xnode text=""ConstructorInfo"" key=""[KeyObject]"" dunk=""[String🡒ConstructorInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#00</key>
          <value>Void .ctor()</value>
        </entry>
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#01</key>
          <value>Void .ctor(Int32, System.String)</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""MethodInfo"" key=""[KeyObject]"" dunk=""[String🡒MethodInfo] Count=05"">
      <values count=""5"">
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#00</key>
          <value>Void Promote(System.String)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#01</key>
          <value>Void Promote(System.String, System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Promote#02</key>
          <value>Void Promote(System.String, System.String, System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>Terminate</key>
          <value>Void Terminate()</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ToString</key>
          <value>System.String ToString()</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=07"">
      <values count=""7"">
        <entry type=""RuntimePropertyInfo"">
          <key>Id</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Name</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Department</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Title</key>
          <value>String</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Salary</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>HireDate</key>
          <value>DateTime</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>IsActive</key>
          <value>Boolean</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""EventInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=00"">
      <values count=""0"" />
    </xnode>
  </xnode>
  <xnode text=""SalaryRecord"" key=""[KeyObject]"">
    <xnode text=""ConstructorInfo"" key=""[KeyObject]"" dunk=""[String🡒ConstructorInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#00</key>
          <value>Void .ctor()</value>
        </entry>
        <entry type=""RuntimeConstructorInfo"">
          <key>.ctor#01</key>
          <value>Void .ctor(Int32, System.Decimal)</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""MethodInfo"" key=""[KeyObject]"" dunk=""[String🡒MethodInfo] Count=04"">
      <values count=""4"">
        <entry type=""RuntimeMethodInfo"">
          <key>ApplyRaise</key>
          <value>Void ApplyRaise(System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ApplyBonus</key>
          <value>Void ApplyBonus(System.Decimal)</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>TotalCompensation</key>
          <value>System.Decimal TotalCompensation()</value>
        </entry>
        <entry type=""RuntimeMethodInfo"">
          <key>ToString</key>
          <value>System.String ToString()</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=05"">
      <values count=""5"">
        <entry type=""RuntimePropertyInfo"">
          <key>EmployeeId</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>BaseSalary</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Bonus</key>
          <value>Decimal</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>EffectiveDate</key>
          <value>DateTime</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>Currency</key>
          <value>String</value>
        </entry>
      </values>
    </xnode>
    <xnode text=""EventInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=00"">
      <values count=""0"" />
    </xnode>
  </xnode>
</model>"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting model to match."
            );

            dunk = Brisk[typeof(EmployeeRecord)];
            Assert.AreEqual(0, dunk.Count, $"Expecting this is 'not' the target. FYI");
            dunk = Brisk[typeof(EmployeeRecord), typeof(PropertyInfo)];
            Assert.AreNotEqual(0, dunk.Count, $"Expecting this 'is' the target. FYI");
            PropertyInfo? pi;
            
            pi = (PropertyInfo)Brisk[typeof(EmployeeRecord), typeof(PropertyInfo)]["Name"]!;
            Assert.IsNotNull(pi);
            Assert.IsInstanceOfType<PropertyInfo>(pi);

            xunk = 
                Brisk[typeof(EmployeeRecord), typeof(PropertyInfo)]
                .ToXDunk(@throw: true)!;

        }
        #endregion S U B T E S T S
    }

    [TestMethod]
    public void Test_StrongTypedSwaps()
    {
        string actual, expected;
        List<string> builder = new();
        BriskDictionaryWrapper bdw;

        StrongTypesUpgradeStatus status;

        using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                BriskReset();
            },
            onDispose: (sender, e) =>
            {
                BriskReset();
            });

        IObservableDictionary
            dunk;

        dunk = Brisk[101];

        subtestAsIsNoChangeNeeded();
        subtestSwapSucceedsOnEmptyDict();
        subtestSwapSucceedsAgainOnEmptyDict();
        subtestUseStrongTypedDict();

        #region S U B T E S T S
        void subtestAsIsNoChangeNeeded()
        {
            // As-is
            dunk = dunk.AsStronglyTypedDictionary<object, object>(out status)!;

            Assert.AreEqual(
                1, BriskDictionary.ReverseLookup.Count,
                $"Expecting that this is the 'one and only' entry.");
            Assert.IsTrue(
                BriskDictionary.ReverseLookup.ContainsKey(dunk),
                $"Expecting that reverse lookup has been updated.");
            Assert.IsTrue(
                dunk.TryGetHost(out bdw),
                $"Expecting host is intact.");

            Assert.IsInstanceOfType<IObservableDictionary<object, object>>(dunk);
            Assert.AreEqual(
                StrongTypesUpgradeStatus.NoChangeNeeded,
                status,
                $"Expecting, specifically, NoChangeNeeded as opposed to Succeeded which indicates a conversion took place.");
            { }
        }
        void subtestSwapSucceedsOnEmptyDict()
        {
            dunk = dunk.AsStronglyTypedDictionary<string, int>(out status)!;

            Assert.AreEqual(
                1, BriskDictionary.ReverseLookup.Count,
                $"Expecting that this is the 'one and only' entry.");
            Assert.IsTrue(
                BriskDictionary.ReverseLookup.ContainsKey(dunk),
                $"Expecting that reverse lookup has been updated.");
            Assert.IsTrue(
                dunk.TryGetHost(out bdw),
                $"Expecting host is intact.");

            Assert.IsInstanceOfType<IObservableDictionary<string, int>>(dunk);
            Assert.AreEqual(
                StrongTypesUpgradeStatus.Succeeded,
                status,
                $"Expecting, specifically, Succeeded which indicates a conversion took place.");
            { }
        }
        void subtestSwapSucceedsAgainOnEmptyDict()
        {
            dunk = dunk.AsStronglyTypedDictionary<Enum, Action>(out status)!;

            Assert.AreEqual(
                1, BriskDictionary.ReverseLookup.Count,
                $"Expecting that this is the 'one and only' entry.");
            Assert.IsTrue(
                BriskDictionary.ReverseLookup.ContainsKey(dunk),
                $"Expecting that reverse lookup has been updated.");
            Assert.IsTrue(
                dunk.TryGetHost(out bdw),
                $"Expecting host is intact.");

            Assert.IsInstanceOfType<IObservableDictionary<Enum, Action>>(dunk);
            Assert.AreEqual(
                StrongTypesUpgradeStatus.Succeeded,
                status,
                $"Expecting, specifically, Succeeded which indicates a conversion took place.");
            { }
        }
        void subtestUseStrongTypedDict()
        {
            #region L o c a l F x 
            using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                Awaited += localOnAwaited;
                Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
            },
            onDispose: (sender, e) =>
            {
                Awaited -= localOnAwaited;
                Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
            });
            void localOnAwaited(object? sender, Threading.AwaitedEventArgs e)
            {
                // As a result of 1. and 2. we wind up here.
                builder.Add(e.Caller);
            }
            void localOnBeginThrowOrAdvise(object? sender, Throw e)
            {
                builder.Add(e.Message);
                e.Handled = true;
            }
            #endregion L o c a l F x

            var dst = dunk.AsStronglyTypedDictionary<Enum, Action>();
            Assert.IsNotNull(dst, $"Expecting casting success");

            // 1. STORE the delegate in a dictionary
            dst[StdTestActions.OnAwaited] = () =>
            {
                // 2. Now it's being invoked as a retrieval.
                this.OnAwaited();
            };

            Assert.AreEqual(
                1,
                Brisk[101].Count,
                $"Just making sure that dst is the same if we re-reference it.");
            { }

            // This should require no casting
            dst[StdTestActions.OnAwaited]?.Invoke();

            // Now Swap Type Attempt Fails As It Should
            builder.Clear();
            var shouldNotSucceed = dst.AsStronglyTypedDictionary<string, int>(@throw: true);

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Keys
Keys
StrongTypesUpgradeStatus.IncompatibleTKey"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );

            builder.Clear();
            var shouldStillSucceed = dst.AsStronglyTypedDictionary<Enum, Action>(@throw: true);


            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            actual.ToClipboardAssert("Expecting builder content to match.");
            { }
            expected = @"";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "No exceptions this time."
            );
        }
        #endregion S U B T E S T S
    }

    [TestMethod]
    public void Test_UpgradeEventTransfer()
    {
        string actual, expected;
        List<string> builder = new();
        IObservableDictionary? dunk;
        BriskReset();


        #region L o c a l F x				
        void localOnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
        {
            builder.Add(e.ToString());
        }

        void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            builder.Add(e.ToString(true));
        }
        #endregion L o c a l F x

        // Run outside the using block, so that we can attach events.
        subtestCreateAndInitializeDunk();

        using ( dunk.WithOnDispose(
                onInit: (sender, e) =>
                {
                    dunk.CollectionChanging += localOnCollectionChanging;
                    dunk.CollectionChanged += localOnCollectionChanged;
                },
                onDispose: (sender, e) =>
                {
                    dunk.CollectionChanging -= localOnCollectionChanging;
                    dunk.CollectionChanged -= localOnCollectionChanged;
                }))
        {
            subtestUntypedDictionary();
            subtestUpgradeAndTryEvents();
        }

        #region S U B T E S T S 
        void subtestCreateAndInitializeDunk()
        {
            dunk = Brisk[typeof(SimpleClass), typeof(PropertyInfo)];

            dunk.AddRange(
                typeof(SimpleClass)
                .GetProperties()
                .Select(_ => new DictionaryEntry(key: _.Name, value: _)));

            actual = Brisk.ViewExpandedModel();

            // LOOK FOR 2 PROPERTIES CONFIRMING THE BATCH WRITE.
            actual.ToClipboardExpected();
            { }
            expected = @" 
<model>
  <xnode text=""SimpleClass"" key=""[KeyObject]"">
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[Object🡒Object] Count=02"">
      <values count=""2"">
        <entry type=""RuntimePropertyInfo"">
          <key>InstanceCount</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>TimeStamp</key>
          <value>DateTimeOffset</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 2 PropertyInfo entries."
            );
        }

        void subtestUntypedDictionary()
        {
            var revert = dunk[nameof(SimpleClass.TimeStamp)];
            dunk.Remove(nameof(SimpleClass.TimeStamp));

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting one OLD item."
            );

            actual = Brisk.ViewExpandedModel();
            actual.ToClipboardExpected();
            actual.ToClipboardAssert("Expecting result to match.");
            { }
            expected = @" 
<model>
  <xnode text=""SimpleClass"" key=""[KeyObject]"">
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[Object🡒Object] Count=01"">
      <values count=""1"">
        <entry type=""RuntimePropertyInfo"">
          <key>InstanceCount</key>
          <value>Int32</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting result to match."
            );

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting one PropertyInfo remains in an UNTYPED dict."
            );

            builder.Clear();
            dunk[nameof(SimpleClass.TimeStamp)] = revert;

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            actual.ToClipboardAssert("Expecting builder content to match.");
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting one NEW item."
            );

            actual = Brisk.ViewExpandedModel();
            actual.ToClipboardExpected();
            { }
            expected = @" 
<model>
  <xnode text=""SimpleClass"" key=""[KeyObject]"">
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[Object🡒Object] Count=02"">
      <values count=""2"">
        <entry type=""RuntimePropertyInfo"">
          <key>InstanceCount</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>TimeStamp</key>
          <value>DateTimeOffset</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 2 PIs are now present in an UNTYPED dict."
            );
        }

        void subtestUpgradeAndTryEvents()
        {
            builder.Clear();
            var upgraded = dunk.AsStronglyTypedDictionary<string, PropertyInfo>(@throw: true);
            var revert = upgraded[nameof(SimpleClass.TimeStamp)];
            upgraded.Remove(nameof(SimpleClass.TimeStamp));

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Remove, NewItems=null, OldItems=1, NewStartingIndex=-1, OldStartingIndex=-1";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting one OLD item."
            );

            actual = Brisk.ViewExpandedModel();
            actual.ToClipboardExpected();
            actual.ToClipboardAssert("Expecting result to match.");
            { }
            expected = @" 
<model>
  <xnode text=""SimpleClass"" key=""[KeyObject]"">
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=01"">
      <values count=""1"">
        <entry type=""RuntimePropertyInfo"">
          <key>InstanceCount</key>
          <value>Int32</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting result to match."
            );

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting model to show strong typed dictionary AND 1 PI only remaining."
            );

            builder.Clear();
            upgraded[nameof(SimpleClass.TimeStamp)] = revert;

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            actual.ToClipboardAssert("Expecting builder content to match.");
            { }
            expected = @" 
Action=NotifyCollectionChangingAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1
Action=NotifyCollectionChangedAction.Add, NewItems=1, OldItems=null, NewStartingIndex=-1, OldStartingIndex=-1";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting one NEW item."
            );

            actual = Brisk.ViewExpandedModel();
            actual.ToClipboardExpected();
            { }
            expected = @" 
<model>
  <xnode text=""SimpleClass"" key=""[KeyObject]"">
    <xnode text=""PropertyInfo"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=02"">
      <values count=""2"">
        <entry type=""RuntimePropertyInfo"">
          <key>InstanceCount</key>
          <value>Int32</value>
        </entry>
        <entry type=""RuntimePropertyInfo"">
          <key>TimeStamp</key>
          <value>DateTimeOffset</value>
        </entry>
      </values>
    </xnode>
  </xnode>
</model>"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting 2 PIs are now present in an UNTYPED dict."
            );
        }
        #endregion S U B T E S T S
    }

    [TestMethod]
    public async Task Test_InlineStrongTyping()
    {
        using var sta = new STARunner(isVisible: false);
        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            #region L o c a l F x 
            using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                BriskReset();
            },
            onDispose: (sender, e) =>
            {
                BriskReset();
            });
            #endregion L o c a l F x

            string actual, expected;
            List<string> builder = new();

            Button
                btn1 = new(),
                btn2 = new();

            IDictionary<string, Setting>
                dsts1,
                dsts2;

            subtestRuleNumberOne();
            subtestRegisterStrongTypes();
            subtestIJW();

            #region S U B T E S T S

            void subtestRuleNumberOne()
            {
                var byRef1 = Brisk[btn1];
                var byRef2 = Brisk[btn2];

                // R U L E    N U M B E R    O N E
                // These must not be the same dictionary.
                Assert.IsFalse(
                    ReferenceEquals(byRef1, byRef2),
                    $"Expecting instances that are not one of [Type, Enum, string] produce unique keys"
                );

                actual = Brisk.Model.ToString();
                actual.ToClipboardExpected();
                { }

                // This is a REPRESENTATION ONLY.
                expected = @" 
<model>
  <xnode text=""Button:$$$$$$$"" dunk=""[[System.Object🡒Object] Count=00]"" />
  <xnode text=""Button:#######"" dunk=""[[System.Object🡒Object] Count=00]"" />
</model>";

                Assert.IsTrue(actual.Contains($"Button:{RuntimeHelpers.GetHashCode(btn1)}"));
                Assert.IsTrue(actual.Contains($"Button:{RuntimeHelpers.GetHashCode(btn2)}"));
                { }
            }

            void subtestRegisterStrongTypes()
            {
                dsts1 =
                    Brisk[btn1]
                    .AsStronglyTypedDictionary<string, Setting>(
                        activationDlgt: () => new Setting(), @throw: true);

                // Set (insistent) on empty collection
                dsts1[nameof(Button.BackColor)].Value = Color.Aqua;

                Assert.AreEqual(
                    1,
                    dsts1.Count,
                    $"Expecting that the ActivatorDlgt makes insistence transparent.");

                Assert.IsInstanceOfType<Setting>(
                    dsts1[nameof(Button.BackColor)],
                    $"Expecting this getter to return an instance of {nameof(Setting)}"
                );

                var loopback =
                    Brisk[btn1].AsStronglyTypedDictionary<string, Setting>();
                Assert.ReferenceEquals(
                    loopback,
                    dsts1);

                Assert.IsNotInstanceOfType<BriskDictionaryWrapper>(
                    dsts1,
                    $"Expecting that this points to {nameof(BriskDictionaryWrapper)}.@base."
                );

                Assert.IsTrue(
                    dsts1.TryGetHost(out var bdw),
                    $"Success here means that the reverse lookup worked."
                );

                var xdunk = dsts1.ToXDunk();
                Assert.IsInstanceOfType<XElement>(
                    xdunk,
                    $"Success here means that the reverse lookup worked yet again."
                );
                dsts1[nameof(Button.BackColor)].Value = Color.Magenta;

                // Now set btn2.
                dsts2 =
                    Brisk[btn2]
                    .AsStronglyTypedDictionary<string, Setting>(
                        activationDlgt: () => new Setting(), @throw: true);

                dsts2[nameof(Button.BackColor)].Value = Color.Aqua;
            }

            void subtestIJW()
            {
                btn1.BackColor = SystemColors.Window;

                // IJW.
                var dstst = Brisk[btn1].SafeAs<string, Setting>();
                btn1.BackColor = dstst[nameof(Button.BackColor)].Value.SafeAs<Color>();

                Assert.AreEqual(Color.Magenta, btn1.BackColor);
            }
            #endregion S U B T E S T S
            await Task.CompletedTask;
        }
    }

    [TestMethod]
    public void Test_KeySegment()
    {
        string actual, expected;
        List<string> builder = new();
        IObservableDictionary? dunk;
        using var local = this.WithOnDispose(
            onInit: (sender, e) =>
            {
                BriskReset();
            },
            onDispose: (sender, e) =>
            {
                BriskReset();
            });

        dunk = Brisk[StdAbsoluteKeyDefault.SimpleClass];

        actual = Brisk.ViewExpandedModel();
        actual.ToClipboardExpected();
        { } // <- FIRST TIME ONLY: Adjust the message.
        actual.ToClipboardAssert("Expecting model to match.");
        { }
        expected = @" 
<model>
  <xnode text=""StdAbsoluteKeyDefault"">
    <xnode text=""SimpleClass"" key=""[KeyObject]"" dunk=""[Object🡒Object] Count=00"">
      <values count=""0"" />
    </xnode>
  </xnode>
</model>";

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting model to match."
        );

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting model to match."
        );

        BriskReset();
        dunk = Brisk[StdCacheReflectionStrongTyped.ButtonWindowsForms];

        actual = Brisk.ViewExpandedModel();
        actual.ToClipboardExpected();
        { }
        expected = @" 
<model>
  <xnode text=""Type"">
    <xnode text=""Object"">
      <xnode text=""ButtonWindowsForms"">
        <xnode text=""Platform"">
          <xnode text=""Buttons"" key=""[KeyObject]"" dunk=""[String🡒PropertyInfo] Count=00"">
            <values count=""0"" />
          </xnode>
        </xnode>
      </xnode>
    </xnode>
  </xnode>
</model>"
        ;

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting model now indicates a strong-typed dictionary."
        );

        actual = dunk.ToFormattedDictName();

        actual.ToClipboardExpected();
        { }
        expected = @" 
(tolerant)[String:PropertyInfo]";

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting IJW typed dictionary"
        );
    }
}
