
#if WINDOWS
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ListView = Microsoft.Maui.Controls.ListView;
#endif

#if ANDROID
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Android.Text.Method;
#endif


using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using OPC.Preview.Maui;
using System.Windows.Input;
using static IVSoftware.Portable.GlyphProvider;
using SQLite;
using Newtonsoft.Json;
using IVSoftware.Portable;
using PointerEventArgs = Microsoft.Maui.Controls.PointerEventArgs;
using System.Diagnostics;
using QueryFilterList.Portable.Demo;
using OPC.Preview.Portable;
using System.ComponentModel;
using System.Transactions;
using OPC.Preview.Portable.Models;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;


namespace QueryFilterList.Maui.Demo
{
    class MainPageBindingContext : MainBindingContext
    {
        public MainPageBindingContext()
        {
            CardPressedCommand = new Command<ItemCardModel>(OnCardPressed);
            PointerMovedCommand = new Command<PointerEventArgs>(OnPointerMoved);
            PointerExitedCommand = new Command<PointerEventArgs>(OnPointerExited);
            CardReleasedCommand = new Command<ItemCardModel>(OnCardReleased);
            ClearCommand = new Command(OnClear);
            TapOverlayCommand = new Command(OnTapOverlay);
            IsCheckedContext.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(TrackContext<ItemCardModel>.CurrentItems):
                        IsCheckboxFilteringEnabled =
                            IsCheckedContext.CurrentItems.Length == 0
                            ? false
                            : IsCheckedContext.CurrentItems.Length == Items.CountUnfiltered
                                ? false
                                : true;
                        break;
                }
            };
        }

        public ICommand CardPressedCommand { get; }
        void OnCardPressed(ItemCardModel? item)
        {
            if (item is not null) SelectionContext.ItemPressed(item);
        }
        public ICommand PointerMovedCommand { get; }
        private void OnPointerMoved(PointerEventArgs e)
        {
            if (SelectionContext.PressedItem is null)
            {   /* G T K */
                // N O O P
            }
            else
            {
                if (PosInitial is null)
                {
                    PosInitial = e.GetPosition(null);
                }
                PosDelta = e.GetPosition(null) - PosInitial;
                if (IsDeltaThresholdMet())
                {
                    SelectionContext.WDTLongPressed.Cancel();
                }
            }
        }
        public ICommand PointerExitedCommand { get; }
        private void OnPointerExited(PointerEventArgs o)
        {
            SelectionContext.CancelItemPressed();
        }
        public ICommand CardReleasedCommand { get; }
        void OnCardReleased(ItemCardModel? item)
        {
            if (item is not null) SelectionContext.ItemReleased(item);
        }

        public ICommand ClearCommand { get; }

        // #{0706F021-9860-4DF9-A4C5-322CE6527DF3}
        private void OnClear(object o)
        {
            // SearchEntryState.QueryEmpty simply means that the text
            // has been cleared. Receiving a second clear command should
            // reset it to SearchEntryState.QueryCleared and that state
            // change is responsible for clearing the collection view.
            // THIS SHOULD NOT BE NECESSARY
            // - It's a bug
            // - The bug lives in the SqliteMarkdownContext lib.
#if true
            bool clearAll =
                Items.MarkdownContext is not null
                && Items.MarkdownContext.SearchEntryState == SearchEntryState.QueryEmpty;
            Items.MarkdownContext?.Clear(clearAll);
#else

            Items.MarkdownContext?.Clear();
#endif
        }

        public ICommand TapOverlayCommand { get; }
        private void OnTapOverlay(object o)
        {
            o.SetModalResult(modalResult: ModalResult.Cancel);
            
        }

        bool IsDeltaThresholdMet(int deltaXY = 10)
            => _posDelta is not null
               &&
               (Math.Abs(((Size)_posDelta).Width) > deltaXY || Math.Abs(((Size)_posDelta).Height) > deltaXY);
        public Point? PosInitial
        {
            get => _posInitial;
            set
            {
                if (!Equals(_posInitial, value))
                {
                    _posInitial = value;
                    OnPropertyChanged();
                }
            }
        }
        Point? _posInitial = default;

        public Size? PosDelta
        {
            get => _posDelta;
            set
            {
                if (!Equals(_posDelta, value))
                {
                    _posDelta = value;
                    //CanReorderItems =
                    //    _posDelta is not null
                    //    &&
                    //    (Math.Abs(((Size)_posDelta).Width) > ReorderThreshold || Math.Abs(((Size)_posDelta).Height) > ReorderThreshold);
                    OnPropertyChanged();
                }
            }
        }
        Size? _posDelta = default;

        public Color SearchBarIconColor
        {
            get
            {
                if(_searchBarIconColor is null)
                {
                    _searchBarIconColor =
                        GetThemeColor(light: Color.Parse("#222222"), dark: Color.Parse("#DDDDDD"));
                }
                return IsAdaptiveShowAll
                ? Colors.Red
                : _searchBarIconColor;
            }
            set
            {
                if (!Equals(_searchBarIconColor, value))
                {
                    _searchBarIconColor = value;
                    OnPropertyChanged();
                }
            }
        }
        Color? _searchBarIconColor = null;

        Color GetThemeColor(Color light, Color dark)
        {
            var theme = AppThemePCL?.ToString();
            switch (theme)
            {
                case null:
                case nameof(AppTheme.Light):
                    return light;
                default:
                    return dark;
            }
        }

        public string SearchBarPlaceholder
        {
            get => _searchBarPlaceholder;
            set
            {
                if (!Equals(_searchBarPlaceholder, value))
                {
                    _searchBarPlaceholder = value;
                    OnPropertyChanged();
                }
            }
        }
        string _searchBarPlaceholder = "Search";

        public bool CanReorderItems
        {
            get => _canReorderItems;
            set
            {
                if (!Equals(_canReorderItems, value))
                {
                    _canReorderItems = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _canReorderItems = false;

        public override TrackContext<ItemCardModel> SelectionContext
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
                    _selectionContext.ModifiersRequest += (sender, e) =>
                    {
                        var modifiers = new List<string>();
#if WINDOWS

                        if (InputKeyboardSource
                            .GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                        {
                            modifiers.Add(nameof(VirtualKey.Control));
                        }

                        if (InputKeyboardSource
                            .GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                        {
                            modifiers.Add(nameof(VirtualKey.Shift));
                        }

                        if (modifiers.Any())
                        {
                            // Only in combination
                            if (InputKeyboardSource
                                .GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                            {
                                modifiers.Add("Alt");
                            }
                        }
#endif
                        e.Modifiers = modifiers.ToArray();
                    };
#if DEBUG
                    _selectionContext.PropertyChanged += (sender, e) =>
                    {
                        switch (e.PropertyName)
                        {
                            case nameof(ITrackContext.CurrentItems):
                                { }
                                break;
                        }
                    };
#endif
                }
                return _selectionContext;
            }
        }

        protected override void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(MarkdownContext.SearchEntryState):
                case nameof(MarkdownContext.FilteringState):
                    switch (Items?.MarkdownContext?.SearchEntryState)
                    {
                        case SearchEntryState.Cleared:
                        case SearchEntryState.QueryEmpty:
                            SearchBarIconColor = GetThemeColor(
                                light: Color.Parse("#222222"), 
                                dark: Color.Parse("#DDDDDD"));
                            // After setting the icon color, check
                            // whether we need to clear the list as well.
                            break;
                        case SearchEntryState.QueryENB:
                            SearchBarIconColor = Colors.LightSalmon;
                            break;
                        case SearchEntryState.QueryEN:
                            SearchBarIconColor = Colors.Green;
                            break;
                        case SearchEntryState.QueryCompleteNoResults:
                        case SearchEntryState.QueryCompleteWithResults:
                            // The search bar icon and icon color depends
                            // on filter eligibility, and if filter mode
                            // isn't available then we leave these alone.
                            switch (Items?.MarkdownContext?.FilteringState)
                            {
                                case FilteringState.Ineligible:
                                    SearchBarIcon = IconBasics.Search.ToGlyph()!;
                                    SearchBarPlaceholder = "Search";
                                    break;
                                case FilteringState.Armed:
                                case FilteringState.Active:
                                    SearchBarIcon = IconBasics.Filter.ToGlyph()!;
                                    SearchBarPlaceholder = "Filter";
                                    SearchBarIconColor = Colors.LightSalmon;
                                    break;
                            }
                            break;
                        default:
                            this.ThrowHard<NotSupportedException>(
                                $"The {Items?.MarkdownContext?.SearchEntryState.ToFullKey()} case is not supported.");
                            break;
                    }
                    // Icon and placeholder text.
                    switch (Items?.MarkdownContext?.FilteringState)
                    {
                        case FilteringState.Ineligible:
                            SearchBarIcon = IconBasics.Search.ToGlyph()!;
                            SearchBarPlaceholder = "Search";
                            break;
                        case FilteringState.Armed:
                        case FilteringState.Active:
                            SearchBarIcon = IconBasics.Filter.ToGlyph()!;
                            SearchBarPlaceholder = "Filter";
                            break;
                    }
                    break;
            }
        }
    }
}
