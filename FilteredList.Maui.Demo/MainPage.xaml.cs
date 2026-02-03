using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;
using System.Collections.Specialized;
using static IVSoftware.Portable.GlyphProvider;
using System.Diagnostics;

using OPC.Preview.Portable.Models;

#if WINDOWS
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
#endif

namespace FilteredList.Maui.Demo
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
#if WINDOWS
            Loaded += (sender, e) => Window!.Title = "Filtered List";
            foreach (var context in BindingContext.Items.TrackContexts.Values)
            {
                context!.PropertyChanged += (sender, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(context.PressedItem):
                            break;
                        case nameof(context.CurrentItems):
                            // Win Title Bar Text
                            int
                                chk = BindingContext.IsCheckedContext.CurrentItems.Length,
                                chkB = BindingContext.Items.Count - chk;
                            Window!.Title =
                                $"Sel={BindingContext.SelectionContext.CurrentItems.Length} " +
                                $"Chk={chk}:{chkB}";
                            break;
                    }
                };
            }
#endif
            Loaded += (sender, e) =>
            {
                BindingContext.PopulateDemoItems();
            };
        }
        new MainPageBindingContext BindingContext => (MainPageBindingContext)base.BindingContext;
    }

    class MainPageBindingContext 
        : INotifyPropertyChanged
        , INotifyPropertyChanging
    {
        public MainPageBindingContext()
        {
            CardPressedCommand = new Command<ItemCardModel>(OnItemPressed);
            CardReleasedCommand = new Command<ItemCardModel>(OnItemReleased);
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
        public ObservablePreviewCollection<ItemCardModel> Items
        {
            get
            {
                if (_items is null)
                {
                    _items = new ObservablePreviewCollection<ItemCardModel>
                    {
                        OptimizationMode =
                        ListOptimizationMode.UseCacheForContains
                        | ListOptimizationMode.TrackItemPropertyChanges
                    };
                    OnPropertyChanged();
#if DEBUG
                    _items.CollectionChanged += (sender, e) =>
                    {
                        Debug.WriteLine($"260110.A {DateTime.Now:ss.ffff} {e.Action}");
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                break;
                            case NotifyCollectionChangedAction.Reset:
                                break;
                        }
                    };
#endif
                }

                return _items;
            }
        }
        ObservablePreviewCollection<ItemCardModel>? _items = null;

        public TrackContext<ItemCardModel> SelectionContext
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
                }
                return _selectionContext;
            }
        }
        TrackContext<ItemCardModel>? _selectionContext = null;

        public TrackContext<ItemCardModel> IsCheckedContext
            => Items.TrackContexts[nameof(ItemCardModel.IsChecked)]!;


        public ICommand CardPressedCommand { get; }
        void OnItemPressed(ItemCardModel? item)
        {
            if(item is not null) SelectionContext.ItemPressed(item);
        }

        public ICommand CardReleasedCommand { get; }
        void OnItemReleased(ItemCardModel? item)
        {
            if (item is not null) SelectionContext.ItemReleased(item);
        }

        public bool ShowCheckboxes => !HideCheckboxes;
        public bool HideCheckboxes
        {
            get => _hideCheckboxes;
            set
            {
                if (!Equals(_hideCheckboxes, value))
                {
                    _hideCheckboxes = value;
                    HideCheckboxesIcon =
                        _hideCheckboxes 
                        ? IconBasics.Hidden 
                        : IconBasics.Shown;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowCheckboxes));
                }
            }
        }
        bool _hideCheckboxes = false;

        public IconBasics HideCheckboxesIcon
        {
            get => _HideCheckboxesIcon;
            set
            {
                if (!Equals(_HideCheckboxesIcon, value))
                {
                    _HideCheckboxesIcon = value;
                    OnPropertyChanged();
                }
            }
        }
        IconBasics _HideCheckboxesIcon = IconBasics.Shown;


        public bool ShowChecked
        {
            get => _showChecked;
            set
            {
                if (!Equals(_showChecked, value))
                {
                    _showChecked = value;
                    if (_showChecked)
                    {
                        ShowUnchecked = false;
                    }
                    OnPropertyChanged();
                }
            }
        }
        bool _showChecked = false;

        public bool ShowUnchecked
        {
            get => _showUnchecked;
            set
            {
                if (!Equals(_showUnchecked, value))
                {
                    _showUnchecked = value;
                    if(_showUnchecked)
                    {
                        ShowChecked = false;
                    }
                    OnPropertyChanged();
                }
            }
        }
        bool _showUnchecked = false;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HideCheckboxes):
                case nameof(ShowChecked):
                case nameof(ShowUnchecked):
                    UpdateCheckboxFilters();
                    break;
            }
            if (ReferenceEquals(sender, this))
            {
                PropertyChanged?.Invoke(sender, e);
            }
        }

        private void UpdateCheckboxFilters()
        {
            if (Items is IFilterableCollection filterable)
            {
                using (filterable.BeginFilterAtom())
                {
                    if (ShowChecked ^ ShowUnchecked)
                    {
                        if (ShowChecked)
                        {
                            filterable.ActivateFilters(StdPredicate.IsChecked);
                            filterable.DeactivateFilters(StdPredicate.IsUnchecked);
                        }
                        else
                        {
                            filterable.ActivateFilters(StdPredicate.IsUnchecked);
                            filterable.DeactivateFilters(StdPredicate.IsChecked);
                        }
                    }
                    else
                    {
                        filterable.DeactivateFilters(StdPredicate.IsChecked, StdPredicate.IsUnchecked);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;

        internal void PopulateDemoItems()
        {
            var items = Items;
            int id = 0;
            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Apple",
                Keywords = @"[""fruit"", ""red"", ""sweet""]",
                Tags = "fruit produce",
                IsChecked = true
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Banana",
                Keywords = @"[""fruit"", ""yellow"", ""soft""]",
                Tags = "fruit produce",
                IsChecked = false
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Carrot",
                Keywords = @"[""vegetable"", ""orange"", ""root""]",
                Tags = "vegetable produce",
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Broccoli",
                Keywords = @"[""vegetable"", ""green"", ""cruciferous""]",
                Tags = "vegetable produce",
                IsChecked = true
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Strawberry",
                Keywords = @"[""fruit"", ""red"", ""berry""]",
                Tags = "fruit produce berry",
                IsChecked = false
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Spinach",
                Keywords = @"[""vegetable"", ""leafy"", ""green""]",
                Tags = "vegetable produce leafy",
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Orange",
                Keywords = @"[""fruit"", ""citrus"", ""orange""]",
                Tags = "fruit produce citrus",
                IsChecked = true
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Tomato",
                Keywords = @"[""fruit"", ""red"", ""savory""]",
                Tags = "fruit vegetable produce",
                IsChecked = false
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Cucumber",
                Keywords = @"[""vegetable"", ""green"", ""fresh""]",
                Tags = "vegetable produce",
            });

            items.Add(new ItemCardModel
            {
                Id = $"{id++}",
                Description = "Blueberry",
                Keywords = @"[""fruit"", ""blue"", ""small""]",
                Tags = "fruit produce berry",
                IsChecked = true
            });
        }
    }
}
