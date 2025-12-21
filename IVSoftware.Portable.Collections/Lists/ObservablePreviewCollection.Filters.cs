using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using SQLite;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> 
        : IFilterableCollection
        , INotifyPropertyChanging
    {
        public IReadOnlyList<Enum> ActiveFilters => ActiveFiltersProtected.Values.OfType<Enum>().ToArray();

        public TolerantDictionary<string, Enum> ActiveFiltersProtected
        {
            get
            {
                if (_activeFiltersProtected is null)
                {
                    _activeFiltersProtected = new TolerantDictionary<string, Enum>();
                    _activeFiltersProtected.CollectionChanging += (sender, e) =>
                    {
                        switch (e.Action)
                        {
                            case NotifyCollectionChangingAction.Add:
                                if(_activeFiltersProtected.Count == 0)
                                {
                                    ItemsSourceProtected.Clear();
                                    ItemsSourceProtected.AddRange(this);
                                }
                                break;
                        }
                    };
                    _activeFiltersProtected.CollectionChanged += (sender, e) =>
                    {
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                            case NotifyCollectionChangedAction.Remove:
                            case NotifyCollectionChangedAction.Reset:
                                OnPropertyChanged(nameof(ActiveFilters));
                                break;
                        }
                    };
                }
                return _activeFiltersProtected;
            }
        }
        TolerantDictionary<string, Enum>? _activeFiltersProtected = null;

        internal DisposableHost DHostActiveFilterAtomic
        {
            get
            {
                if (_dhostActiveFilterAtomic is null)
                {
                    _dhostActiveFilterAtomic = new DisposableHost();
                    
                    _dhostActiveFilterAtomic.BeginUsing += (sender, e) =>
                    { };

                    _dhostActiveFilterAtomic.FinalDispose += (sender, e) =>
                    {
                        ReconcileFilters();
                    };
                }
                return _dhostActiveFilterAtomic;
            }
        }
        DisposableHost? _dhostActiveFilterAtomic = null;

        /// <summary>
        /// Make radio button interactions atomic
        /// </summary>
        public IDisposable BeginFilterAtom() => DHostActiveFilterAtomic.GetToken();

        private bool TryGetTable(out string table)
        {
            if (string.IsNullOrWhiteSpace(_table))
            {
                var query = "SELECT name FROM sqlite_master WHERE type ='table' AND name NOT LIKE 'sqlite_%';";
                var tableNames = FilterDB.QueryScalars<string>(query);
                if (tableNames.Count == 1)
                {
                    _table = tableNames[0];
                }
                else
                {
                    this.ThrowFramework<SQLiteException>("Expecting a Single table for type T.");
                }
            }
            table = _table ?? string.Empty;
            return !string.IsNullOrWhiteSpace(table);
        }
        private string? _table = null;

        private bool TryGetPrimaryKeyProperty(out PropertyInfo primaryKey)
        {
            if (_isPrimaryKeyChecked)
            {
                primaryKey = PrimaryKeyProperty!; // Returns false if null
            }
            else
            {
                PrimaryKeyProperty ??=
                        typeof(T)
                        .GetProperties()
                        .FirstOrDefault(_ => _.GetCustomAttributes().Any(attr => attr.GetType().Name.StartsWith("PrimaryKey")))!;
                primaryKey = PrimaryKeyProperty!;
                _isPrimaryKeyChecked = true;
            }
            return primaryKey is not null;
        }
        private bool _isPrimaryKeyChecked = false;
        public PropertyInfo? PrimaryKeyProperty { get; protected set; } = null!;

        public bool IsTNew
        {
            get
            {
                if (_isTNew is null)
                {
                    var type = typeof(T);
                    _isTNew =
                        !type.IsAbstract &&
                        !type.IsInterface &&
                        (
                            type.GetConstructor(Type.EmptyTypes) is not null
                            || type.IsValueType   // structs always have an implicit default ctor
                        );
                }
                return (bool)_isTNew;
            }
        }
        bool? _isTNew = null;

        private void ReconcileFilters()
        {
            NotifyPreviewCollectionChangedEventArgs e;

            try
            {
                // Do it HERE do it NOW for all clauses.
                base.Clear();
                FilterDB.DeleteAll<T>();
                FilterDB.InsertAll(ItemsSourceProtected);

                if (ActiveFilters.Any()
                    && string.IsNullOrEmpty(MarkdownContext?.InputText)
                    && TryGetTable(out var table)
                    && TryGetPrimaryKeyProperty(out var pi))
                {
                    var predicates =
                        ActiveFilters
                        .Select(_ => _.GetCustomAttribute<WhereAttribute>()?.Expr)
                        .Where(_ => !string.IsNullOrWhiteSpace(_))
                        .Select(_ => $"({_})")// Add parentheses out of an abundance of paranoia.
                        .ToList();
                    if (MarkdownContext?.XAST.Attribute(nameof(StdAstAttr.clauseE))?.Value is string markdown)
                    {
                        predicates.Insert(0, markdown);
                        Debug.Fail($@"ADVISORY - First Time.");
                    }

                    var predicateWhere = string.Join(" AND ", predicates);
                    // Some predicate or predicates.
                    var sql = $"SELECT {pi.Name} FROM {table} WHERE {predicateWhere}";

                    var pkVisible = new HashSet<string>();

                    // What should be visible.
                    // This is culled from the ItemsSourceInternal that is the OG
                    // authority because it was captured when the first filter came on.
                    TableMapping map = FilterDB.GetMapping(typeof(T));
                    foreach (var item in FilterDB.Query(map, sql))
                    {
                        var pk = pi.GetValue(item)?.ToString();
                        if (!string.IsNullOrWhiteSpace(pk))
                        {
                            pkVisible.Add(pk);
                        }
                    }
                    List<T> visibleItems = new();
                    if (pkVisible.Any())
                    {
                        foreach (T item in ItemsSourceProtected)
                        {
                            if (pi.GetValue(item)?.ToString() is { } pk
                                && !string.IsNullOrWhiteSpace(pk)
                                && pkVisible.Contains(pk))
                            {
                                base.Add(item);
                            }
                        }
                        goto breakFromInner;
                    }
                }
                // None is "Show All"
                foreach (T item in ItemsSourceProtected)
                {
                    base.Add(item);
                }
            }
            finally
            {
                Distinctifier.SyncReset();
            }

            breakFromInner:
            using (DHostSuspendTracking.GetToken())
            {
                e = new(NotifyCollectionChangedAction.Reset);
                OnCollectionChanged(e);
            }
            foreach (var context in FollowContexts.Values)
            {
                context!.UpdateCurrentItemsArray();
            }
            FiltersReconciled?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler? FiltersReconciled;

        public async Task<IReadOnlyList<Enum>> ActivateFilters(Enum filter, params Enum[] moreFilters)
        {
            string binding, predicate;
            TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            FiltersReconciled += localOnFiltersReconciled;
            void localOnFiltersReconciled(object? sender, EventArgs e)
            {
                FiltersReconciled -= localOnFiltersReconciled;
                tcs.TrySetResult();
            }

            if (filter.TryGetWhere(out binding, out predicate, @throw: true))
            {
                ActiveFiltersProtected[binding] = filter;
            }
            foreach (var more in moreFilters)
            {
                if (more.TryGetWhere(out binding, out predicate, @throw: true))
                {
                    ActiveFiltersProtected[binding] = more;
                }
            }
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            return ActiveFilters;
        }
        public async Task<IReadOnlyList<Enum>> DeactivateFilters(Enum filter, params Enum[] moreFilters)
        {
            string binding, predicate;
            if (filter.TryGetWhere(out binding, out predicate, @throw: true))
            {
                // Retrieve the current property-bound predicate...
                if (ActiveFiltersProtected[binding] is { } found)
                {
                    // ... but don't remove it unless it's a MATCH for the remove request.
                    if (Equals(found, filter))
                    {
                        ActiveFiltersProtected.Remove(binding);
                    }
                    else
                    {   /* G T K */
                        // This was a BUGIRL. Fixed now.
                    }
                }
            }
            foreach (var more in moreFilters)
            {
                if (more.TryGetWhere(out binding, out predicate, @throw: true))
                {
                    if (ActiveFiltersProtected[binding] is { } found && Equals(found, more))
                    {
                        ActiveFiltersProtected.Remove(binding);
                    }
                }
            }
            return ActiveFilters;
        }

        public void ClearFilters()
        {
            if (ActiveFiltersProtected.Any())
            {
                ActiveFiltersProtected.Clear();
                OnPropertyChanged(nameof(ActiveFilters));
            }
        }
        protected virtual void OnItemPropertyChanging(object? sender, PropertyChangingEventArgs e)
        {
            OnPropertyChanging(new ItemPropertyChangingEventArgs(item: sender, e));
        }

        protected virtual void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(new ItemPropertyChangedEventArgs(item: sender, e));
        }

        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            if (Suppressed.HasFlag(SuppressionFlag.PropertyChanging))
            {
                EventSuppressed?.Invoke(this, e);
            }
            else
            {
                PropertyChanging?.Invoke(this, e);
                var cancel = e is PropertyChangingPreviewEventArgs<T> eItem && eItem.Cancel;
                if (!cancel)
                {
                    switch (e.PropertyName)
                    {
                        case nameof(ActiveFilters):
                            if(ActiveFilters.Count == 0)
                            {

                            }
                            break;
                        case nameof(IsFiltering):
                            break;
                    }
                }
            }
        }
        public event PropertyChangingEventHandler? PropertyChanging;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (Suppressed.HasFlag(SuppressionFlag.PropertyChanged))
            {
                EventSuppressed?.Invoke(this, e);               
            }
            else
            {
                base.OnPropertyChanged(e);
                switch (e.PropertyName)
                {
                    case nameof(ActiveFilters):
                    case nameof(IsFiltering):
                        if(ActiveFilters.Any())
                        {
                            WDTActiveFilterSettle.StartOrRestart(e);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Make this public again. (In the BC it's not)
        /// </summary>
        public new event PropertyChangedEventHandler? PropertyChanged
        {
            add => base.PropertyChanged += value;
            remove => base.PropertyChanged -= value;
        }

        WatchdogTimer WDTActiveFilterSettle
        {
            get
            {
                if (_wdtActiveFilterSettle is null)
                {
                    _wdtActiveFilterSettle = new WatchdogTimer { Interval = TimeSpan.FromSeconds(0.1) };
                    _wdtActiveFilterSettle.PropertyChanged += (sender, e) =>
                    { 
                    };
                    _wdtActiveFilterSettle.RanToCompletion += (sender, eUnk) =>
                    {
                        if (eUnk is PropertyChangedEventArgs e)
                        {
                            if (ActiveFiltersProtected.Any())
                            {
                                ReconcileFilters();
                            }
                        }
                    };
                }
                return _wdtActiveFilterSettle;
            }
        }
        WatchdogTimer? _wdtActiveFilterSettle = null;

        public int CountUnfiltered => IsFiltering ? ItemsSourceProtected.Count : Count;

        public bool IsFiltering => ActiveFilters.Any() || MarkdownContext?.IsFiltering == true;
    }    
}
