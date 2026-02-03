#if WINDOWS
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
#endif

#if ANDROID
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Android.Text.Method;
#endif

using IVSoftware.Portable.SQLiteMarkdown;
using OPC.Preview.Maui.Controls;
using static IVSoftware.Portable.GlyphProvider;
using IVSoftware.Portable.Collections;
using OPC.Preview.Portable;
using Application = Microsoft.Maui.Controls.Application;
using System.Diagnostics;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using OPC.Preview.Portable.Models;
using QueryFilterList.Portable.Demo;
using OPC.Preview.Portable.Events;
using OPC.Preview.Maui.Views;


namespace QueryFilterList.Maui.Demo
{
    public partial class MainPage
        : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            AppTheme appTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            // Commit to portable binding context as string.
            BindingContext.AppThemePCL = appTheme;
#if WINDOWS
            #region W I N D O W S 
            Loaded += (sender, e) =>
            {
                Window!.Title = "Query + Filter Demo";
            };
            MarkdownContext.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(MarkdownContext.SearchEntryState):
                    case nameof(MarkdownContext.FilteringState):
                        if (MarkdownContext.FilteringState == FilteringState.Ineligible)
                        {
                            Window!.Title = $"{MarkdownContext.SearchEntryState}";
                        }
                        else
                        {
                            Window!.Title = $"{MarkdownContext.SearchEntryState}.{MarkdownContext.FilteringState}";
                        }
                        break;
                }
            };
            #endregion W I N D O W S
#endif
            Loaded += async(sender, e) => await InitializeAsync();

            // [Probationary]
            BindingContext.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(BindingContext.ModalMultiConfiguration):
                        if(BindingContext
                           .ModalMultiConfiguration
                           .Any(_=>_==typeof(SetCheckedGroup) || _ == typeof(ShowCheckedStateGroup)))
                        {
                            // Could be cleaner, but the idea is that once these modal
                            // groups have been shown via a long press, it obviates
                            // the need to display it on the "first checkbox toggle".
                            InfoOverlay.SetDSA(StdInfo.CheckBoxPrompt);
                        }
                        break;
                }
            };
#if DEBUG
            BindingContext.Items.OptimizationMode |= ListOptimizationMode.TrackItemPropertyChanges;
            BindingContext.Items.PropertyChanged += (sender, eUnk) =>
            {
                if (eUnk is ItemPropertyChangedEventArgs e)
                {
                    switch (e.PropertyName)
                    {
                        case "PressedSelection":
                            Debug.WriteLine($"260108.A Pressed={((ItemCardModel)e.Item!).Selection.ToFullKey()}");
                            break;
                        case "Selection":
                            Debug.WriteLine($"260108.A Selection={((ItemCardModel)e.Item!).Selection.ToFullKey()}");
                            break;
                        default:
                            Debug.WriteLine($"260108.A {eUnk.PropertyName}");
                            break;
                    }
                }
            };
#endif
        }

        async Task InitializeAsync()
        {
            await BoostCache();
            BindingContext.LoadedCommand?.Execute(null);
#if false && SAVE
            // https://github.com/fontello/fontello/issues/791
            // Generate one enum definition per config.json discovered in the assembly.
            // Many apps have more than one font kit, and multiple bundles will produce multiple enums.
            string[] prototypes = await GlyphProvider.CreateEnumPrototypes();

            Debug.Assert(
                prototypes.Any(),
                "You should also see prototypes for any additional config.json files " +
                "that you've marked as Embedded Resource. (Note: in WPF, this must be " +
                "EmbeddedResource - not Resource - for discovery to work.)"
            );

            var enumsGen =
                string.Join(
                    $"{Environment.NewLine}{Environment.NewLine}",
                    prototypes);

            { } // < Set a debug break HERE to copy the `enumsGen` from text visualizer to your code.
#endif
        }
        new MainPageBindingContext BindingContext => (MainPageBindingContext)base.BindingContext;
        MarkdownContext MarkdownContext => BindingContext.Items.MarkdownContext!;

        private void OnScroll(object sender, ItemsViewScrolledEventArgs e)
        {
            BindingContext.SelectionContext.CancelItemPressed();
            BindingContext.SelectionContext.WDTLongPressed.Cancel();
        }
    }
}
