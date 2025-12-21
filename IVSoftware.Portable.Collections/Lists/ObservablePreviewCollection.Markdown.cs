
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.SQLiteMarkdown;
using SQLite;
using System.Collections.Specialized;
using System.Reflection;

namespace IVSoftware.Portable.Collections.Lists
{
    partial class ObservablePreviewCollection<T>
    {
        public MarkdownContext<T>? MarkdownContext
        {
            get
            {
                if (_markdownContext is null && IsTNew && TryGetPrimaryKeyProperty(out _))
                {
                    _markdownContext = new MarkdownContext<T>();
                }
                return _markdownContext;
            }
        }
        MarkdownContext<T>? _markdownContext = null;


        public SQLiteConnection FilterDB
        {
            get
            {
                if (TryGetPrimaryKeyProperty(out _))
                {
                    if (_filterDB is null)
                    {
                        _filterDB = new SQLiteConnection(":memory:");
                        _filterDB.CreateTable<T>();
                    }
                }
                else
                {
                    this.ThrowSoft<InvalidOperationException>("Unnecessary singleton instantiation for FilterDB for ineligible config.");
                }
                return _filterDB!;
            }
        }
        SQLiteConnection? _filterDB = null;
    }
}
