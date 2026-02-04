using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.Xml.Linq;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.Portable.Xml.Linq.XBoundObject.Modeling;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OPC.Preview.Portable;
using OPC.Preview.Portable.Events;
using OPC.Preview.Portable.Models;
using SQLite;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Input;
using static IVSoftware.Portable.GlyphProvider;
using static System.Net.Mime.MediaTypeNames;

namespace QueryFilterList.Portable.Demo
{
    /// <summary>
    /// Portable class library for QueryFilterList Demo.
    /// </summary>
    public class MainBindingContext : PortableBindingContext
    {
        public MainBindingContext() 
        {
            // [Careful]
            // *Not* a good candidate for singleton.
            // This needs to be pushed in and available e.g. for debug init.
            DemoDB = CreateDemoDB();
            LoadedCommand = new CommandPCL(OnLoaded);
            SearchBarIconTappedCommand = new CommandPCL(OnSearchBarIconTapped);
            ClickableEventCommand = new CommandPCL<ClickableEventArgs>(OnClickableEvent);
            CardCheckBoxClickableEventCommand = new CommandPCL<ClickableEventArgs>(OnCardCheckBoxClickableEvent);
            CommitCommand = new CommandPCL<MarkdownContext>(OnCommit);

            ModalResultCommittedEventArgs.ModalResultCommitted += (sender, e) =>
            {
                OnClickableEvent(new ClickableEventArgs(ClickableEventType.Released) { OPID = e.Result});
            };
        }

        public override async Task SinkClickableEvent(object sender, ClickableEventArgs e)
        {
            await base.SinkClickableEvent(sender, e);
            switch (e.EventType)
            {
                case ClickableEventType.Pressed:
                    break;
                case ClickableEventType.Clicked:
                    switch (sender)
                    {
                        case PropertyEditorModel model:
                            switch (e.OPID)
                            {
                                case ApplyCancel.Apply:
                                    using (DHostBusy.GetToken())
                                    using (Items.DHostUIActivity.GetToken())
                                    {
                                        if (model.Item is ItemCardModel item)
                                        {
                                            switch (ModalStack.Peek())
                                            {
                                                case EditingCommands.Add:
                                                    Items.Add(item);
                                                    item.Selection = ItemSelection.Exclusive;
                                                    DemoDB.Insert(item);
                                                    break;
                                                case EditingCommands.Edit:
                                                    var existing = Items.First(i => i.Id == item.Id);
                                                    if (ReferenceEquals(item, existing))
                                                    {
                                                        // WORKS EVERY TIME
                                                    }
                                                    else
                                                    {
                                                        throw new InvalidOperationException("Expecting reference is equal");
                                                    }
                                                    DemoDB.Update(item);
                                                    break;
                                                case EditingCommands.Delete:
                                                    throw new NotImplementedException("ToDo - Action list confirmation of delete.");
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
#if false
                                        if (e.Sender is IOPItemEditor editor)
                                        {
                                            if (editor.Item is ItemCardModel item)
                                            {
                                                switch (ModalStack.Peek())
                                                {
                                                    case EditingCommands.Add:
                                                        Items.Add(item);
                                                        item.Selection = ItemSelection.Exclusive;
                                                        DemoDB.Insert(item);
                                                        break;
                                                    case EditingCommands.Edit:
                                                        var existing = Items.First(i => i.Id == item.Id);
                                                        if (ReferenceEquals(item, existing))
                                                        {
                                                            // WORKS EVERY TIME
                                                        }
                                                        else
                                                        {
                                                            throw new InvalidOperationException("Expecting reference is equal");
                                                        }
                                                        DemoDB.Update(item);
                                                        break;
                                                    case EditingCommands.Delete:
                                                        throw new NotImplementedException("ToDo");
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                        PopModalOPID(ApplyCancel.Apply);
#endif
                                    }
                            break;
                                case ApplyCancel.Cancel:
                                    break;
                            }
                            IsPropertyEditorVisible = false;
                            break;
                    }
                    break;
                case ClickableEventType.LongPressed:
                    break;
                case ClickableEventType.Released:
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// ItemCardModel
        /// </summary>
        public ObservablePreviewCollection<ItemCardModel> Items
        {
            get
            {
                if (_items is null)
                {
                    _items = new();
                    _items.AmbientBindingContext = this;
                    _items.PropertyChanged += (sender, eUnk) =>
                    {
                        if (eUnk is ItemPropertyChangedEventArgs e)
                        {
                            switch (e.PropertyName)
                            {
                                case nameof(ItemCardModel.Selection):
                                    if (e.Item is not null)
                                    {
                                        PropertyEditorItem = e.Item;
                                    }
                                    break;
                            }
                        }
                    };
                    IsCheckedContext.PropertyChanged += (sender, e) =>
                    {
                        switch (e.PropertyName)
                        {
                            case nameof(IsCheckedContext.CurrentItems):
                                // Info alert for checked items.
                                if(IsCheckedContext.CurrentItems.Length == 1)
                                {
                                    this.StdAlert = StdInfo.CheckBoxPrompt;
                                }
                                break;
                            default:
                                break;
                        }
                    };
                    OnPropertyChanged(nameof(SelectionContext));

                    // [Careful]
                    // Type of T must meet requirements and if it doesn't 
                    // then expect _items.MarkdownContext to stay null.
                    if (_items.MarkdownContext is not null)
                    {
                        // Merge MDC property stream into main INPC.
                        _items.MarkdownContext.PropertyChanged += (sender, e) =>
                        {
                            OnPropertyChanged(e.PropertyName);
                        };
                    }

                    _items.BeforeAdaptiveShowAll += (sender, e) 
                        => OnBeforeAdaptiveShowAll(e);
                }
                return _items;
            }
        }

        ObservablePreviewCollection<ItemCardModel>? _items = null;

        protected override void PushModalOPID(Enum opid)
        {
            base.PushModalOPID(opid);
            switch (opid)
            {
                case EditingCommands.Add:
                    PropertyEditorItem = new ItemCardModel
                    {
                        Description = "New Item",
                    };
                    IsPropertyEditorVisible = true;
                    break;
                case EditingCommands.Edit:
                    PropertyEditorItem = SelectionContext.CurrentItems.Single();
                    IsPropertyEditorVisible = true;
                    break;
                case EditingCommands.Delete:
                    var deletes = SelectionContext.CurrentItems;
                    Items.RemoveMultiple(deletes);
                    foreach (var delete in deletes)
                    {
                        DemoDB.Delete(delete);
                    }
                    PopModalOPID(opid);
                    break;
                default:
                    Debug.Fail($"NotImplementedException: '{opid}'");
                    break;
            }
        }
        protected override (object? sender, Enum result)? PopModalOPID(Enum result)
        {
            var tuple = base.PopModalOPID(result);
            switch (tuple?.sender)
            {
                case EditingCommands:
                    IsPropertyEditorVisible = false;
                    break;
                default:
                    Debug.Fail($"NotImplementedException: '{tuple?.sender}'");
                    break;
            }
            return tuple;
        }

        public ICommand LoadedCommand { get; }
        private void OnLoaded(object? o)
        {
#if DEBUG && true
            // For debug, do an initial query.
            Items.MarkdownContext!.InputText = "green";
            Items.MarkdownContext.GetAwaiter().OnCompleted(() =>
            {
                OnCommit(Items.MarkdownContext);
            });
#endif

            // For production, display an initial prompt.
#if DEBUG
            Debug.Assert(DateTime.Now.Date == new DateTime(2026, 2, 04).Date, "Don't forget disabled");
            // StdAlert = StdInfo.InfoTextQueryPrompt;
#else
            StdAlert = StdInfo.InfoTextQueryPrompt;
#endif

            Items.MarkdownContext.InputTextSettled += localOnInputTextSettled;
            void localOnInputTextSettled(object? sender, EventArgs e)
            {
                if (Items.MarkdownContext.InputText.Length == 0)
                {
                    switch (Items.MarkdownContext.FilteringState)
                    {
                        case FilteringState.Ineligible:
                            break;
                        case FilteringState.Armed:
                            Items.MarkdownContext.InputTextSettled -= localOnInputTextSettled;
                            StdAlert = StdInfo.FilterClearPrompt;
                            break;
                        case FilteringState.Active:
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public ICommand SearchBarIconTappedCommand { get; }
        private void OnSearchBarIconTapped(object o)
        {
            if (IsAdaptiveShowAll)
            {
                Items.WDTReconcileFilters.StartOrRestart();
            }
        }
        public ICommand ClickableEventCommand { get; }
        private void OnClickableEvent(ClickableEventArgs e)
        {
            // In general...
            e.Handled = true;

            switch (e.EventType)
            {
                case ClickableEventType.Released:
                    switch(e.OPID)
                    {
                        case null:
                        default:
                            this.ThrowHard<NotSupportedException>($"The {e.OPID.ToFullKey()} case is not supported.");
                            break;
                        case EditingCommands:
                            PushModalOPID(e.OPID);
                            break;

                        case ApplyCancel.Cancel:
                            PopModalOPID(ApplyCancel.Cancel);
                            break;

                        // S H O W
                        case ShowCheckedStateGroup.Checked:
                            using (Items.BeginFilterAtom())
                            {
                                Items.ActivateFilters(StdPredicate.IsChecked);
                                Items.DeactivateFilters(StdPredicate.IsUnchecked);
                            }
                            break;
                        case ShowCheckedStateGroup.Unchecked:
                            using (Items.BeginFilterAtom())
                            {
                                Items.ActivateFilters(StdPredicate.IsUnchecked);
                                Items.DeactivateFilters(StdPredicate.IsChecked);
                            }
                            break;
                        case ShowCheckedStateGroup.All:
                            using (Items.BeginFilterAtom())
                            {
                                Items.DeactivateFilters(StdPredicate.IsChecked);
                                Items.DeactivateFilters(StdPredicate.IsUnchecked);
                            }
                            break;

                        // S E T
                        case SetCheckedGroup.CheckAll:
                            foreach (var item in Items)
                            {
                                item.IsChecked = true;
                            }
                            break;
                        case SetCheckedGroup.UncheckAll:
                            foreach (var item in Items)
                            {
                                item.IsChecked = false;
                            }
                            break;
                        case ModalResult.Cancel:
                            break;
                    }
                    break;
            }
        }
        public bool IsPropertyEditorVisible
        {
            get => _isPropertyEditorVisible;
            set
            {
                if (!Equals(_isPropertyEditorVisible, value))
                {
                    _isPropertyEditorVisible = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isPropertyEditorVisible = false;

        public object PropertyEditorItem
        {
            get => _propertyEditorItem;
            set
            {
                if (!Equals(_propertyEditorItem, value))
                {
                    _propertyEditorItem = value;
                    OnPropertyChanged();
                }
            }
        }
        object _propertyEditorItem = typeof(ItemCardModel);

        protected virtual void OnBeforeAdaptiveShowAll(BeforeAdaptiveShowAllEventArgs e)
        {
            if (IsAdaptiveShowAll)
            {
                e.Cancel = false;
                IsAdaptiveShowAll = false;
            }
            else
            {
                IsAdaptiveShowAll = e.IsEmpty;
            }
        }

        public bool IsAdaptiveShowAll
        {
            get => _isAdaptiveShowAll;
            set
            {
                if (!Equals(_isAdaptiveShowAll, value))
                {
                    _isAdaptiveShowAll = value;
                    OnPropertyChanged();
                    OnPropertyChanged("SearchBarIconColor");
                    if(_isAdaptiveShowAll)
                    {
                        StdAlert = StdInfo.AdaptiveShowAllPrompt;
                    }
                }
            }
        }
        bool _isAdaptiveShowAll = false;

        public Settings Settings { get; } = new();

        public virtual TrackContext<ItemCardModel> SelectionContext
        {
            get
            {
                if (_selectionContext is null)
                {
                    _selectionContext = Items.TrackContexts[nameof(ItemCardModel.Selection)]!;
                    _selectionContext.LongPressed += (sender, e) =>
                    {
                        HideCheckboxes = !HideCheckboxes;
                    };
                }
                return _selectionContext;
            }
        }
        protected TrackContext<ItemCardModel>? _selectionContext = null;

        public ICommand CommitCommand { get; }
        protected async void OnCommit(MarkdownContext mdc)
        {
            using (DHostBusy.GetToken(sender: StdModalView.ActivityIndicator))
            {
                var stopwatch = Stopwatch.StartNew();
                List<ItemCardModel> recordset = null!;
                await Task.Run(() =>
                {
                    var sql = Items.MarkdownContext!.ParseSqlMarkdown();
                    Debug.WriteLine($"260109.A Elapsed={stopwatch.ElapsedMilliseconds}");
                    recordset = DemoDB.Query<ItemCardModel>(sql);
                    Debug.WriteLine($"260109.A Elapsed={stopwatch.ElapsedMilliseconds}");
                    if (recordset.Count == 0 && Settings[StdSetting.AllowPluralize].SafeAs<bool>())
                    {
                        sql = sql.ToFuzzyQuery();
                        recordset = DemoDB.Query<ItemCardModel>(sql);
                    }
                });
                if (recordset is not null)
                {
                    Items.Recordset = recordset;
                    Debug.WriteLine($"260109.A Elapsed={stopwatch.ElapsedMilliseconds}");
                }
            }
        }
        public ICommand CardCheckBoxClickableEventCommand { get; }
        private void OnCardCheckBoxClickableEvent(ClickableEventArgs e)
        {
            switch (e.EventType)
            {
                case ClickableEventType.Pressed:
                    OnCardCheckBoxPressed(e);
                    break;
                case ClickableEventType.Clicked:
                    break;
                case ClickableEventType.LongPressed:
                    OnCardCheckBoxLongPressed(e);
                    break;
                case ClickableEventType.Released:
                    OnCardCheckBoxReleased(e);
                    break;
                default:
                    break;
            }
        }
        public WatchdogTimer WDTIsBusyPreDelay
        {
            get
            {
                if (_wdtPreDelay is null)
                {
                    _wdtPreDelay = new WatchdogTimer(defaultCompleteAction: () =>
                    {
                        _checkBoxPressedToken = DHostBusy.GetToken(sender: BusyMinimumDelay.Disabled);
                    })
                    { Interval = TimeSpan.FromSeconds(0.2) };
                }
                return _wdtPreDelay;
            }
        }
        WatchdogTimer? _wdtPreDelay = null;
        private async void OnCardCheckBoxPressed(object o)
        {
            WDTIsBusyPreDelay.StartOrRestart();
        }
        private void OnCardCheckBoxLongPressed(object o)
        {
            PushModal([typeof(ShowCheckedStateGroup), typeof(SetCheckedGroup),]);
            ModalMultiConfiguration = [typeof(ShowCheckedStateGroup), typeof(SetCheckedGroup),];
        }
        IDisposable? _checkBoxPressedToken = null;
        private void OnCardCheckBoxReleased(object o)
        {
            WDTIsBusyPreDelay.Cancel();
            var dispose = _checkBoxPressedToken;
            _checkBoxPressedToken = null;
            dispose?.Dispose();
        }

        public Enum? StdAlert
        {
            get => _StdAlert;
            set
            {
                if (!Equals(_StdAlert, value))
                {
                    _StdAlert = value;
                    OnPropertyChanged();
                }
            }
        }
        Enum? _StdAlert = null;

        /// <summary>
        /// Auto-Reset property that always fires.
        /// </summary>
        /// <remarks>
        /// Configuration caching is the responsibility of classes like
        /// ModalCollectionView. But this model reports every setter call.
        /// </remarks>
        public Type[] ModalMultiConfiguration
        {
            get => _modalMultiConfiguration;
            set
            {
                value ??= []; // Out of an abundance of caution.
                _modalMultiConfiguration = value;
                OnPropertyChanged(nameof(ModalConfiguration));
                OnPropertyChanged(nameof(ModalMultiConfiguration));
                _modalMultiConfiguration = [];
            }
        }
        Type[] _modalMultiConfiguration = [];

        /// <summary>
        /// Satisfy the IConfigurable contract through 
        /// the IMultiConfiguration implementation.
        /// </summary>
        public Type? ModalConfiguration
        {
            get =>
                ModalMultiConfiguration.Length == 1
                ? ModalMultiConfiguration[0]
                : null;
            set => ModalMultiConfiguration =
                value is null
                ? []
                : [value];
        }

        public bool IsBusy => WDTIsBusyMin.Running || !DHostBusy.IsZero();

        public WatchdogTimer WDTIsBusyMin
        {
            get
            {
                if (_wdtIsBusyMin is null)
                {
                    _wdtIsBusyMin = new WatchdogTimer { Interval = TimeSpan.FromSeconds(0.5) };
                    _wdtIsBusyMin.RanToCompletion += (sender, e) =>
                    {
                        OnPropertyChanged(nameof(IsBusy));
                    };
                }
                return _wdtIsBusyMin;
            }
        }
        WatchdogTimer? _wdtIsBusyMin = null;

        public DisposableHost DHostBusy
        {
            get
            {
                if (_dhostBusy is null)
                {
                    _dhostBusy = new DisposableHost(nameof(DHostBusy));
                    _dhostBusy.BeginUsing += (sender, e) =>
                    {
                        bool useMinDelay = _dhostBusy.Tokens.FirstOrDefault()?.Sender switch 
                        {
                            BusyMinimumDelay.Disabled => false,
                            BusyMinimumDelay.Enabled => true,
                            _ => true, 
                        };
                        if(useMinDelay) WDTIsBusyMin.StartOrRestart();
                        OnPropertyChanged(nameof(IsBusy));
                    };
                    _dhostBusy.FinalDispose += (sender, e) =>
                        OnPropertyChanged(nameof(IsBusy));
                }
                return _dhostBusy;
            }
        }
        DisposableHost? _dhostBusy = null;

        public bool ShowCheckboxes => !HideCheckboxes;
        public bool HideCheckboxes
        {
            get => _hideCheckboxes;
            set
            {
                if (!Equals(_hideCheckboxes, value))
                {
                    _hideCheckboxes = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowCheckboxes));
                }
            }
        }
        bool _hideCheckboxes = true;

        /// <summary>
        /// Tracks the Checked:Unchecked ratio when ShowCheckboxes is true.
        /// </summary>
        public bool IsCheckboxFilteringEnabled
        {
            get => _isCheckboxFilteringEnabled;
            set
            {
                if (!Equals(_isCheckboxFilteringEnabled, value))
                {
                    _isCheckboxFilteringEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isCheckboxFilteringEnabled = false;

        public TrackContext<ItemCardModel> IsCheckedContext
            => Items.TrackContexts[nameof(ItemCardModel.IsChecked)]!;

        public string InputText
        {
            get => _inputText;
            set
            {
                if (!Equals(_inputText, value))
                {
                    _inputText = value;
                    OnPropertyChanged();
                }
            }
        }
        string _inputText = string.Empty;

        public string SearchBarIcon
        {
            get => _searchBarIcon;
            set
            {
                if (!Equals(_searchBarIcon, value))
                {
                    _searchBarIcon = value;
                    OnPropertyChanged();
                }
            }
        }
        string _searchBarIcon = IconBasics.Search.ToGlyph()!;

        public SQLiteConnection DemoDB { get; }
        public SQLiteConnection CreateDemoDB()
        {
            if (_demoDB is null)
            {
                _demoDB = new SQLiteConnection(":memory:");
                _demoDB.CreateTable<ItemCardModel>();

                var list = new List<ItemCardModel>();

                void Add(string description, string tags, bool isChecked, List<string>? keywords = null)
                {
                    var instance = new ItemCardModel();
                    instance.Description = description;
                    instance.Tags = tags;
                    instance.IsChecked = isChecked;
                    if (keywords != null)
                    {
                        var json = JsonConvert.SerializeObject(keywords);
                        instance.Keywords = json;
                    }
                    list.Add(instance);
                }
                Add("Brown Dog", "[canine] [color]", false, new() { "loyal", "friend", "furry" });
                Add("Green Apple", "[fruit] [color]", false, new() { "tart", "snack", "healthy" });
                Add("Yellow Banana", "[fruit] [color]", false);
                Add("Blue Bird", "[bird] [color]", false, new() { "sky", "feathered", "song" });
                Add("Red Cherry", "[fruit] [color]", false, new() { "sweet", "summer", "dessert" });
                Add("Black Cat", "[animal] [color]", false);
                Add("Orange Fox", "[animal] [color]", false);
                Add("White Rabbit", "[animal] [color]", false, new() { "bunny", "soft", "jump" });
                Add("Purple Grape", "[fruit] [color]", false);
                Add("Gray Wolf", "[animal] [color]", false, new() { "pack", "howl", "wild" });
                Add("Pink Flamingo", "[bird] [color]", false);
                Add("Golden Lion", "[animal] [color]", false);
                Add("Brown Bear", "[animal] [color]", false, new() { "strong", "wild", "forest" });
                Add("Green Pear", "[fruit] [color]", false);
                Add("Red Strawberry", "[fruit] [color]", false);
                Add("Black Panther", "[animal] [color]", false, new() { "stealthy", "feline", "night" });
                Add("Yellow Lemon", "[fruit] [color]", false);
                Add("White Swan", "[bird] [color]", false);
                Add("Purple Plum", "[fruit] [color]", false);
                Add("Blue Whale", "[marine-mammal] [ocean]", false, new() { "ocean", "mammal", "giant" });
                Add("Elephant", "[animal]", false, new() { "trunk", "herd", "safari" });
                Add("Pineapple", "[fruit]", false);
                Add("Shark", "[fish]", false);
                Add("Owl", "[bird]", false);
                Add("Giraffe", "[animal]", false);
                Add("Coconut", "[fruit]", false);
                Add("Kangaroo", "[animal]", false, new() { "bounce", "outback", "marsupial" });
                Add("Dragonfruit", "[fruit]", false);
                Add("Turtle", "[animal]", false);
                Add("Mango", "[fruit]", false);
                Add("Should NOT match an expression with an \"animal\" tag.", "[not animal]", false);

                // Live-demo specific.
                Add("Appetizer Plate", "[dish]", false, new() { "starter", "appealing", "snack" });
                Add("Errata", "[notes]", false, new() { "crunchy", "green", "appended" });
                Add("Happy Camper", "[phrase]", false, new() { "joyful", "camp", "approach-west" });
                Add("Great example - Markdown Demo", "[app] [portable]", false, new() { "digital", "mobile", "software" });
                Add("Application Form", "[document]", false, new() { "paperwork", "apply" });
                Add("App Store", "[app]", false, new() { "digital", "mobile", "software" });

                _demoDB.InsertAll(list);
            }
            return _demoDB;
        }
        SQLiteConnection? _demoDB = null;

        protected override void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                default:
                    break;
            }
        }
    }
}
