using FilteredList.Maui.Demo.Models;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Collections.TrackingContexts;
using IVSoftware.Portable.SQLiteMarkdown;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

#if WINDOWS
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
#endif
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;
using SelectionMode = Microsoft.Maui.Controls.SelectionMode;
using System.Collections.Specialized;

namespace FilteredList.Maui.Demo
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += (sender, e) => Window!.Title = "Filtered List";
            foreach (var context in BindingContext.ItemsSource.TrackContexts.Values)
            {
                context!.PropertyChanged += (sender, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(context.PressedItem):
                            break;
                        case nameof(context.CurrentItems):
#if WINDOWS
                            // Win Title Bar Text
                            int
                                chk = BindingContext.IsCheckedContext.CurrentItems.Length,
                                chkB = BindingContext.ItemsSource.Count - chk;
                            Window!.Title =
                                $"Sel={BindingContext.SelectionContext.CurrentItems.Length} " +
                                $"Chk={BindingContext.IsCheckedContext.CurrentItems.Length}:{BindingContext.IsCheckedContext.CurrentItemsInverted.Length}";
                            break;
                    }
                };
            }
        }
        new MainPageBindingContext BindingContext => (MainPageBindingContext)base.BindingContext;
    }

    class MainPageBindingContext 
        : INotifyPropertyChanged
        , INotifyPropertyChanging
    {
        public ICommand ItemPressedCommand { get; }
        public ICommand ItemReleasedCommand { get; }
        public MainPageBindingContext()
        {
            ItemPressedCommand = new Command<ItemCardModel>(OnItemPressed);
            ItemReleasedCommand = new Command<ItemCardModel>(OnItemReleased);

            // Populate demo.
            foreach (var item in PopulateDemoItems().OfType<ItemCardModel>())
            {
                ItemsSource.Add(item);
            }
        }
        public ObservablePreviewCollection<ItemCardModel> ItemsSource
        {
            get
            {
                if (_itemsSource is null)
                {
                    _itemsSource = new ObservablePreviewCollection<ItemCardModel>
                    {
                        OptimizationMode =
                        ListOptimizationMode.UseCacheForContains
                        | ListOptimizationMode.TrackItemPropertyChanges
                    };
                    OnPropertyChanged();

#if DEBUG
                    _itemsSource.CollectionChanged += (sender, e) =>
                    {
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                break;
                            case NotifyCollectionChangedAction.Remove:
                                break;
                            case NotifyCollectionChangedAction.Replace:
                                break;
                            case NotifyCollectionChangedAction.Move:
                                break;
                            case NotifyCollectionChangedAction.Reset:
                                { }
                                break;
                            default:
                                break;
                        }
                    };
#endif
                }
                return _itemsSource;
            }
        }
        ObservablePreviewCollection<ItemCardModel>? _itemsSource = null;

        public TrackContext<ItemCardModel> SelectionContext
        {
            get
            {
                if (_selectionContext is null)
                {
                    _selectionContext = ItemsSource.TrackContexts[nameof(ItemCardModel.Selection)]!;
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
            => ItemsSource.TrackContexts[nameof(ItemCardModel.IsChecked)]!;

        void OnItemPressed(ItemCardModel? item)
        {
            if(item is not null) SelectionContext.ItemPress(item);
        }

        void OnItemReleased(ItemCardModel? item)
        {
            if (item is not null) SelectionContext.ItemRelease(item);
        }

        public bool ShowCheckboxes
        {
            get => _showCheckboxes;
            set
            {
                if (!Equals(_showCheckboxes, value))
                {
                    _showCheckboxes = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _showCheckboxes = true;

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
                case nameof(ShowCheckboxes):
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
            if (ItemsSource is IFilterableCollection filterable)
            {
                using (filterable.BeginFilterAtom())
                {
                    if (ShowCheckboxes && (ShowChecked ^ ShowUnchecked))
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

        private static IList PopulateDemoItems()
        {
            var items = new List<ItemCardModel>();
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
            return items;
        }
    }
}
