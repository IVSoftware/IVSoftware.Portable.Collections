using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.SQLiteMarkdown.Common;
using IVSoftware.WinOS.MSTest.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SQLite;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections.MSTest;

[TestClass]
public class TestClass_SQLiteMarkdownContext
{
    [TestMethod]
    public void Test_ContextInstance()
    {
        string actual, expected;

        var mdc = new MarkdownContext<SQLiteMarkdown.Common.SelectableQFModel>();

        var sql = mdc.ParseSqlMarkdown<SQLiteMarkdown.Common.SelectableQFModel>("animal");

        actual = sql;
        actual.ToClipboardExpected();
        { }
        expected = @" 
SELECT * FROM items WHERE 
(QueryTerm LIKE '%animal%')";

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting expression on QueryTerm."
        );
    }

    [TestMethod]
    public void Test_ModelInstance()
    {
        string actual, expected;

        var model = new SelectableQFModel
        {
            Id = "1",
            Description = "Purple Animal",
            Tags = "animal color"
        };
        { }

        actual = JsonConvert.SerializeObject(model, Formatting.Indented);
        actual.ToClipboardExpected();
        { }
        expected = @" 
{
  ""Id"": ""1"",
  ""Description"": ""Purple Animal"",
  ""Keywords"": ""[]"",
  ""KeywordsDisplay"": """",
  ""Tags"": ""[animal][color]"",
  ""IsChecked"": false,
  ""Selection"": 0,
  ""IsEditing"": false,
  ""PrimaryKey"": ""1"",
  ""QueryTerm"": ""purple~animal~[animal][color]"",
  ""FilterTerm"": ""purple~animal~[animal][color]"",
  ""TagMatchTerm"": ""[animal][color]"",
  ""Properties"": ""{\r\n  \""Description\"": \""Purple Animal\"",\r\n  \""Tags\"": \""[animal][color]\""\r\n}""
}"
        ;

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting correct tag representation in Query and Filter exprs."
        );
    }

    [TestMethod]
    public async Task Test_MarkdownContextA()
    {
        string actual, expected;

        var mdc = new MarkdownContext<SelectableQFModel>();
        SemaphoreSlim awaiter = new SemaphoreSlim(0, 1);

        #region L o c a l F x				
        using var local = mdc.WithOnDispose(
            onInit: (sender, e) =>
            {
                mdc.InputTextSettled += localOnInputTextSettled;
            },
            onDispose: (sender, e) =>
            {
                mdc.InputTextSettled -= localOnInputTextSettled;
            });
        void localOnInputTextSettled(object? sender, EventArgs e)
        {
            if(sender is MarkdownContext mdc)
            {
                mdc.ParseSqlMarkdown();
            }
            awaiter.Release();
        }
        #endregion L o c a l F x

        mdc.InputText = "animal";
        await awaiter.WaitAsync();
        { }

        actual = mdc.Query;
        actual.ToClipboardExpected();
        { }
        expected = @" 
SELECT * FROM items WHERE 
(QueryTerm LIKE '%animal%')";

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting query with preamble."
        );
        var where = mdc.XAST.Attribute(nameof(StdAstAttr.clauseE))?.Value ?? "Error";
        actual = where;

        actual.ToClipboardExpected();
        { }
        expected = @" 
(QueryTerm LIKE '%animal%')";

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting where clause only."
        );
    }

#if false && SAVE
    [TestMethod]
    public void Test_MarkdownFilter()
    {
        string actual, expected;

        var list = new MarkdownListProto<SelectableQFModel>();

        actual = JsonConvert.SerializeObject(list, Formatting.Indented);
        actual.ToClipboardExpected();
        { }
        expected = @" 
[
  {
    ""Id"": ""0"",
    ""Description"": ""Apple"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""sweet\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""sweet\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsChecked"": true,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""0"",
    ""QueryTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""FilterTerm"": ""apple~fruit~red~sweet~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Apple\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""sweet\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""Id"": ""1"",
    ""Description"": ""Banana"",
    ""Keywords"": ""[\""fruit\"", \""yellow\"", \""soft\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""yellow\"", \""soft\"""",
    ""Tags"": ""[fruit][produce]"",
    ""IsChecked"": false,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""1"",
    ""QueryTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""FilterTerm"": ""banana~fruit~yellow~soft~[fruit][produce]"",
    ""TagMatchTerm"": ""[fruit][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Banana\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""yellow\\\"", \\\""soft\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce]\""\r\n}""
  },
  {
    ""Id"": ""2"",
    ""Description"": ""Carrot"",
    ""Keywords"": ""[\""vegetable\"", \""orange\"", \""root\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""orange\"", \""root\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsChecked"": false,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""2"",
    ""QueryTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""FilterTerm"": ""carrot~vegetable~orange~root~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Carrot\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""orange\\\"", \\\""root\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  },
  {
    ""Id"": ""3"",
    ""Description"": ""Broccoli"",
    ""Keywords"": ""[\""vegetable\"", \""green\"", \""cruciferous\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""green\"", \""cruciferous\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsChecked"": true,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""3"",
    ""QueryTerm"": ""broccoli~vegetable~green~cruciferous~[vegetable][produce]"",
    ""FilterTerm"": ""broccoli~vegetable~green~cruciferous~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Broccoli\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""green\\\"", \\\""cruciferous\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  },
  {
    ""Id"": ""4"",
    ""Description"": ""Strawberry"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""berry\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""berry\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsChecked"": false,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""4"",
    ""QueryTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""FilterTerm"": ""strawberry~fruit~red~berry~[fruit][produce][berry]"",
    ""TagMatchTerm"": ""[fruit][produce][berry]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Strawberry\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""berry\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][berry]\""\r\n}""
  },
  {
    ""Id"": ""5"",
    ""Description"": ""Spinach"",
    ""Keywords"": ""[\""vegetable\"", \""leafy\"", \""green\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""leafy\"", \""green\"""",
    ""Tags"": ""[vegetable][produce][leafy]"",
    ""IsChecked"": false,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""5"",
    ""QueryTerm"": ""spinach~vegetable~leafy~green~[vegetable][produce][leafy]"",
    ""FilterTerm"": ""spinach~vegetable~leafy~green~[vegetable][produce][leafy]"",
    ""TagMatchTerm"": ""[vegetable][produce][leafy]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Spinach\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""leafy\\\"", \\\""green\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce][leafy]\""\r\n}""
  },
  {
    ""Id"": ""6"",
    ""Description"": ""Orange"",
    ""Keywords"": ""[\""fruit\"", \""citrus\"", \""orange\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""citrus\"", \""orange\"""",
    ""Tags"": ""[fruit][produce][citrus]"",
    ""IsChecked"": true,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""6"",
    ""QueryTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""FilterTerm"": ""orange~fruit~citrus~[fruit][produce][citrus]"",
    ""TagMatchTerm"": ""[fruit][produce][citrus]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Orange\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""citrus\\\"", \\\""orange\\\""]\"",\r\n  \""Tags\"": \""[fruit][produce][citrus]\""\r\n}""
  },
  {
    ""Id"": ""7"",
    ""Description"": ""Tomato"",
    ""Keywords"": ""[\""fruit\"", \""red\"", \""savory\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""red\"", \""savory\"""",
    ""Tags"": ""[fruit][vegetable][produce]"",
    ""IsChecked"": false,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""7"",
    ""QueryTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""FilterTerm"": ""tomato~fruit~red~savory~[fruit][vegetable][produce]"",
    ""TagMatchTerm"": ""[fruit][vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Tomato\"",\r\n  \""Keywords\"": \""[\\\""fruit\\\"", \\\""red\\\"", \\\""savory\\\""]\"",\r\n  \""Tags\"": \""[fruit][vegetable][produce]\""\r\n}""
  },
  {
    ""Id"": ""8"",
    ""Description"": ""Cucumber"",
    ""Keywords"": ""[\""vegetable\"", \""green\"", \""fresh\""]"",
    ""KeywordsDisplay"": ""\""vegetable\"", \""green\"", \""fresh\"""",
    ""Tags"": ""[vegetable][produce]"",
    ""IsChecked"": false,
    ""Selection"": 0,
    ""IsEditing"": false,
    ""PrimaryKey"": ""8"",
    ""QueryTerm"": ""cucumber~vegetable~green~fresh~[vegetable][produce]"",
    ""FilterTerm"": ""cucumber~vegetable~green~fresh~[vegetable][produce]"",
    ""TagMatchTerm"": ""[vegetable][produce]"",
    ""Properties"": ""{\r\n  \""Description\"": \""Cucumber\"",\r\n  \""Keywords\"": \""[\\\""vegetable\\\"", \\\""green\\\"", \\\""fresh\\\""]\"",\r\n  \""Tags\"": \""[vegetable][produce]\""\r\n}""
  },
  {
    ""Id"": ""9"",
    ""Description"": ""Blueberry"",
    ""Keywords"": ""[\""fruit\"", \""blue\"", \""small\""]"",
    ""KeywordsDisplay"": ""\""fruit\"", \""blue\"", \""small\"""",
    ""Tags"": ""[fruit][produce][berry]"",
    ""IsChecked"": true,
    ""Selection"": 0,
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
            "Expecting json serialization to match."
        );
        list.IsFiltered = true;
        actual = JsonConvert.SerializeObject(list, Formatting.Indented);
        actual.ToClipboardExpected();
        { }
        expected = "NOT EMPTY";

        Assert.AreEqual(
            expected.NormalizeResult(),
            actual.NormalizeResult(),
            "Expecting filtered items checked only."
        );
    }

    class FilteredListProto<T> : ObservableCollection<T>, IEnumerable
    {
        public FilteredListProto() 
        {
            foreach (var item in PopulateDemoItems().OfType<T>())
            {
                Add(item);
            }
        }
        public bool IsFiltered
        {
            get => _isFiltered;
            set
            {
                if (!Equals(_isFiltered, value))
                {
                    _isFiltered = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isFiltered = false;

        public new IEnumerator<T> GetEnumerator()
        {
            if(IsFiltered)
            {
                return 
                    Items
                    .OfType<SelectableQFModel>()
                    .Where(_=>_.IsChecked)
                    .Cast<T>()
                    .GetEnumerator();
            }
            else
            {
                return base.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
 
    class MarkdownListProto<T> : ObservableCollection<T>, IEnumerable
    {
        readonly MarkdownContext<T> _mdc = new();
        public SQLiteConnection MemDB
        {
            get
            {
                if (_memDB is null)
                {
                    _memDB = new SQLiteConnection(":memory:");
                    _memDB.CreateTable<T>();
                }
                return _memDB;
            }
        }
        SQLiteConnection? _memDB = null;
        public MarkdownListProto() 
        {
            var items = PopulateDemoItems().OfType<T>();
            foreach (var item in items)
            {
                Add(item);
            }
            MemDB.InsertAll(items);
        }
        public bool IsFiltered
        {
            get => _isFiltered;
            set
            {
                if (!Equals(_isFiltered, value))
                {
                    _isFiltered = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isFiltered = false;

        public new IEnumerator<T> GetEnumerator()
        {
            if(IsFiltered)
            {
                var sql = $"SELECT * FROM items WHERE IsChecked=1";

                HashSet<string> fast = new();
                foreach(var item in MemDB.Query<SelectableQFModel>(sql))
                {
                    fast.Add(item.Id);
                }
                return
                    Items
                    .Cast<SelectableQFModel>()
                    .Where(_ => fast.Contains(_.Id))
                    .Cast<T>()
                    .GetEnumerator();
            }
            else
            {
                return base.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
#endif

    private static IList PopulateDemoItems()
    {
        var items = new List<SelectableQFModel>();
        int id = 0;
        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Apple",
            Keywords = @"[""fruit"", ""red"", ""sweet""]",
            Tags = "fruit produce",
            IsChecked = true
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Banana",
            Keywords = @"[""fruit"", ""yellow"", ""soft""]",
            Tags = "fruit produce",
            IsChecked = false
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Carrot",
            Keywords = @"[""vegetable"", ""orange"", ""root""]",
            Tags = "vegetable produce",
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Broccoli",
            Keywords = @"[""vegetable"", ""green"", ""cruciferous""]",
            Tags = "vegetable produce",
            IsChecked = true
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Strawberry",
            Keywords = @"[""fruit"", ""red"", ""berry""]",
            Tags = "fruit produce berry",
            IsChecked = false
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Spinach",
            Keywords = @"[""vegetable"", ""leafy"", ""green""]",
            Tags = "vegetable produce leafy",
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Orange",
            Keywords = @"[""fruit"", ""citrus"", ""orange""]",
            Tags = "fruit produce citrus",
            IsChecked = true
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Tomato",
            Keywords = @"[""fruit"", ""red"", ""savory""]",
            Tags = "fruit vegetable produce",
            IsChecked = false
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Cucumber",
            Keywords = @"[""vegetable"", ""green"", ""fresh""]",
            Tags = "vegetable produce",
        });

        items.Add(new SelectableQFModel
        {
            Id = $"{id++}",
            Description = "Blueberry",
            Keywords = @"[""fruit"", ""blue"", ""small""]",
            Tags = "fruit produce berry",
            IsChecked = true
        });
        return items;
    }
}
