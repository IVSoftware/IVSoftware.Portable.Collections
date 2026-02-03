using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.Threading;
using SQLite;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections.Lists
{
    public partial class ObservablePreviewCollection<T> 
        : IFilterableCollection
        , INotifyPropertyChanging
    {
        public IReadOnlyDictionary<string, Enum> ActiveFilters
        {
            get
            {
                if (_activeFilters is null)
                {
                    _activeFilters = new ReadOnlyDictionary<string, Enum>(ActiveFiltersProtected!);
                }
                return _activeFilters;
            }
        }
        IReadOnlyDictionary<string, Enum>? _activeFilters = null;

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
                                IsFiltering = true;
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
                                IsFiltering = 
                                    MarkdownContext?.FilteringState == FilteringState.Active 
                                    || ActiveFilters.Count > 0;
                                OnPropertyChanged(nameof(ActiveFilters));
                                _activeFilters = new ReadOnlyDictionary<string, Enum>(_activeFiltersProtected!);
                                break;
                        }
                    };
                }
                return _activeFiltersProtected;
            }
        }
        TolerantDictionary<string, Enum>? _activeFiltersProtected = null;

        int PredicateCount
        {
            get => _predicateCount;
            set
            {
                if (!Equals(_predicateCount, value))
                {
                    _predicateCount = value;
                    OnPredicateCountChanged();
                    OnPropertyChanged();
                }
            }
        }

        private void OnPredicateCountChanged()
        {
        }

        int _predicateCount = default;


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
                        if (IsFiltering)
                        {
                            WDTReconcileFilters.StartOrRestart();
                        }
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
        private readonly object _lock = new();



        public void ActivateFilters(Enum filter, params Enum[] moreFilters)
        {
            foreach (var member in new[] { filter }.Concat(moreFilters))
            {
                if (filter.GetCustomAttribute<WhereAttribute>()?.Binding is { } propertyName && !string.IsNullOrWhiteSpace(propertyName))
                {
                    ActiveFiltersProtected[propertyName] = filter;
                }
            }
        }
        public void DeactivateFilters(Enum filter, params Enum[] moreFilters)
        {
            string binding, predicate;
            if (filter.TryGetWhereAttribute(out binding, out predicate, @throw: true))
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
                if (more.TryGetWhereAttribute(out binding, out predicate, @throw: true))
                {
                    if (ActiveFiltersProtected[binding] is { } found && Equals(found, more))
                    {
                        ActiveFiltersProtected.Remove(binding);
                    }
                }
            }
        }

        /// <summary>
        /// Remove active filters, and optionally clear the MD input text field.
        /// </summary>
        /// <remarks>
        /// MD will handle its own state, but the premise is that an MD that is
        /// armed for filtering will remain so and not break out into query mode.
        /// </remarks>
        public void ClearFilters(bool clearInputText = true)
        {
            if (ActiveFiltersProtected.Any())
            {
                ActiveFiltersProtected.Clear();
                OnPropertyChanged(nameof(ActiveFilters));
            }
            if (clearInputText)
            {
                if (MarkdownContext is not null)
                {
                    // Let MD handle its own state here.
                    MarkdownContext.InputText = string.Empty;
                }
            }
        }
        private async Task ReconcileFilters()
        {
            if (!DHostActiveFilterAtomic.IsZero())
            {
                // Reconciliation is being deferred until DHost releases
                // last token, and this will restart the WDT.
                return;
            }
            else
            {
                if (localConcurrentIsWdtRestarted())
                {
                    Debug.Fail($@"ADVISORY - This is a 'legal-but-rare' corner case.");
                    return;
                }
                try
                {
                    if (IsFiltering)
                    {
                        if (MarkdownContext is null)
                        {
                            // [Track] predicates (non-sql) can still be used,
                            // but this has not been implemented at this time.
                            this.ThrowHard<NotSupportedException>(
                                $"Missing {nameof(MarkdownContext)}");
                        }
                        else
                        {
                            string[] 
                                stagedPKs = [], 
                                thisPKs = [];
                            string? markdown = null;
                            List<T> staged = new();
                            List<string> predicates = new();
                            await Task.Run(() =>
                            {
                                if (MarkdownContext?.FilteringState == FilteringState.Active)
                                {
                                    _ = MarkdownContext.ParseSqlMarkdown();
                                    markdown = MarkdownContext?.GetCurrentFilterPredicate();

                                    if (!string.IsNullOrWhiteSpace(markdown))
                                    {
                                        predicates.Insert(0, markdown);
                                    }
                                }

                                if (localConcurrentIsWdtRestarted()) return;

                                if (ActiveFilters.Any())
                                {
                                    predicates.AddRange(
                                        ActiveFilters.Values
                                        .Select(_ => _.GetCustomAttribute<WhereAttribute>()?.Expr)
                                        .Where(_ => !string.IsNullOrWhiteSpace(_))
                                        .Select(_ => $"({_})"));// Add parentheses out of an abundance of paranoia.
                                }

                                if (predicates.Any()
                                    && FilterDB.GetMapping(typeof(T)) is TableMapping map
                                    && TryGetTable(out var table)
                                    && TryGetPrimaryKeyProperty(out var pi))
                                {
                                    var predicateWhere = string.Join(" AND ", predicates);

                                    // Some predicate or predicates.
                                    var sql = $"SELECT {pi.Name} FROM {table} WHERE {predicateWhere}";

                                    // [Careful]
                                    // The staged property must contain EXISTING REFERENCES.
                                    // The purpose of the query is to obtain the Ids of matches ONLY.
                                    var ids =
                                        FilterDB
                                        .Query(map, sql)
                                        .Select(_ => pi.GetValue(_)?.ToString())
                                        .Where(_ => !string.IsNullOrWhiteSpace(_))
                                        .ToArray(); // For vis
                                    var pkVisible = new HashSet<string>(ids!);

                                    // What should be visible.
                                    // This is culled from the ItemsSourceInternal that is the OG
                                    // authority because it was captured when the first filter came on.
                                    staged = UnfilteredItems.Where(_ =>
                                    {
                                        return
                                            pi.GetValue(_)?.ToString() is { } id
                                            && !string.IsNullOrWhiteSpace(id)
                                            && pkVisible.Contains(id);
                                    }).ToList();
                                }

                                if (localConcurrentIsWdtRestarted()) return;

                                // Provide an opportunity for user to 
                                // 
                                var e = new BeforeAdaptiveShowAllEventArgs(isEmpty: staged.Count == 0);
                                BeforeAdaptiveShowAll?.Invoke(this, e);
                                if (!e.Cancel)
                                {
                                    foreach (T item in UnfilteredItemsProtected)
                                    {
                                        staged.Add(item);
                                    }
                                }

                                stagedPKs =
                                    staged
                                    .Select(_ => PrimaryKeyProperty?.GetValue(_)?.ToString())
                                    .OfType<string>()
                                    .ToArray();
                                thisPKs =
                                    this
                                    .Select(_ => PrimaryKeyProperty?.GetValue(_)?.ToString())
                                    .OfType<string>()
                                    .ToArray();

                            });
                            if (stagedPKs.SequenceEqual(thisPKs))
                            {   /* G T K */
                            }
                            else
                            {
                                // Perform actions only when changes occur.
                                using (DHostSuspendTracking.GetToken())
                                {
                                    base.Clear();
                                    foreach (var item in staged)
                                    {
                                        base.Add(item);
                                    }
                                    // [Careful]
                                    // Remember this must be the Preview subclass.
                                    // Swallowed otherwise!
                                    var ePreview = new NotifyPreviewCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                                    OnCollectionChanged(ePreview);
                                }
                                foreach (var context in TrackContexts.Values)
                                {
                                    context!.SyncReset();
                                }
                                await 
                                    Task
                                    .Run(() => Distinctifier.SyncReset())
                                    .ConfigureAwait(false);
                                this.OnAwaited();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.RethrowFramework(ex);
                }
            }
            #region L o c a l F x 
            bool localConcurrentIsWdtRestarted()
            {
                bool isWdtRestarted;
                lock (_lock)
                {
                    isWdtRestarted = WDTReconcileFilters.Running;
                }
                if (isWdtRestarted)
                {
                    Debug.Fail($@"ADVISORY - Expecting this is a rare corner case.");
                }
                return isWdtRestarted;
            }
            #endregion L o c a l F x
        }
        public event EventHandler<BeforeAdaptiveShowAllEventArgs>? BeforeAdaptiveShowAll;

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

        protected override void OnPropertyChanged(PropertyChangedEventArgs eUnk)
        {
            if (Suppressed.HasFlag(SuppressionFlag.PropertyChanged))
            {
                EventSuppressed?.Invoke(this, eUnk);               
            }
            else
            {
                base.OnPropertyChanged(eUnk);
                if (eUnk is ItemPropertyChangedEventArgs e)
                {
                    if (TrackItemPropertyChanges && ActiveFilters[e.PropertyName!] is not null)
                    {
                        FilterDB.Update(e.Item);
                        WDTReconcileFilters.StartOrRestart();
                    }
                }
                else
                {
                    switch (eUnk.PropertyName)
                    {
                        case nameof(ActiveFilters):
                        case nameof(IsFiltering):
                            WDTReconcileFilters.StartOrRestart();
                            break;
                    }
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

#if false
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
#endif

        public int CountUnfiltered => IsFiltering ? UnfilteredItemsProtected.Count : Count;

        /// <summary>
        /// Transition from false->true will capture recordset.
        /// </summary>
        public bool IsFiltering
        {
            get => _isFiltering;
            set
            {
                if (!Equals(_isFiltering, value))
                {
                    _isFiltering = value;
                    OnIsFilteringChanged();
                    OnPropertyChanged();
                }
            }
        }
        bool _isFiltering = false;

        private void OnIsFilteringChanged()
        {
            if (_isFiltering)
            {
                UnfilteredItemsProtected.AddRange(this.ToArray());
                if (MarkdownContext is not null)
                {
                    FilterDB.InsertAll(UnfilteredItemsProtected);
                }
            }
            else
            {
                Clear();
                if (MarkdownContext is not null)
                {
                    FilterDB.DeleteAll<T>();
                }
                AddRange(UnfilteredItemsProtected);
                UnfilteredItemsProtected.Clear();
            }
        }
        public IReadOnlyList<T> UnfilteredItems => UnfilteredItemsProtected;
        protected List<T> UnfilteredItemsProtected { get; } = new();

        public WatchdogTimer WDTReconcileFilters
        {
            get
            {
                if (_wdtSettle is null)
                {
                    _wdtSettle = new WatchdogTimer(
                        defaultInitialAction: () =>
                        {
#if DEBUG
                            _wdtLog.Add($"251230.A WDTReconcileFilters.InitialAction");
#endif
                            _awaiter.Wait(0);
                        },
                        defaultCompleteAction: async() =>
                        {
                            _awaiter.Wait(0);
                            await ReconcileFilters();
                            _awaiter.Release();
#if DEBUG
                            _wdtLog.Add($"251230.A WDTReconcileFilters.CompleteAction");
#endif
                        });
                    _wdtSettle.Interval = TimeSpan.FromSeconds(0.1);
                }
                return _wdtSettle;
            }
        }
        WatchdogTimer? _wdtSettle = null;

#if DEBUG
        List<string> _wdtLog = new();
#endif

        private readonly SemaphoreSlim _awaiter = new (1,1);

        public TaskAwaiter GetAwaiter()
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    if (!_awaiter.Wait(0))
                    {
                        await _awaiter.WaitAsync();
                    }
                }
                finally
                {
                    _awaiter.Release();
                }
            });
            return task.GetAwaiter();
        }
    }
} 
