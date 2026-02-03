using OPC.Preview.Maui;
using OPC.Preview.Maui.Controls;
using static IVSoftware.Portable.GlyphProvider;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using OPC.Preview.Portable;

#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using CommandBar=OPC.Preview.Maui.Controls.CommandBar;
#endif

namespace ConfigurationIntro.Maui.Demo
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
#if WINDOWS
            Loaded += (sender, e) => Window!.Title = "Configuration Demo";
#endif
            CommandBar.ChildClicked += OnItemClicked;
            CommandBar.PropertyChanged += async (sender, e) =>
            {
                switch(e.PropertyName)
                {
                    case nameof(CommandBar.VisibleItems):
                        if(CommandBar.VisibleItems.Length == 0)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(0.5));
                            await InfoOverlay.ShowInfo(
                                StdInfo.InfoTextPreviewLastVisible);
                            await Task.Delay(TimeSpan.FromSeconds(0.5));
                            foreach (var item in CommandBar.Items)
                            {
                                item.IsVisible = true;
                            }
                        }
                        break;
                }
            };
            CommandBar.Configure<HelpCommand>();
        }

        private async void OnItemClicked(object? sender, ItemClickedEventArgs e)
        {
            if (e.Item is GlyphButton btn)
            {
                switch (btn.OPID)
                {
                    case IconBasics.HelpCircledAlt:
                        await InfoOverlay.ShowInfo(
                            StdInfo.InfoTextConfigureT);
                        await Task.Delay(TimeSpan.FromSeconds(0.5));
                        CommandBar.Configure<EditingCommands>();
                        break;
                    default:
                        if (CommandBar.Items.Length == CommandBar.VisibleItems.Length)
                        {
                            if (await InfoOverlay.ShowInfo(
                                info: $"# Clicked: {(e.Item as GlyphButton)?.OPID.ToFullKey()}{Environment.NewLine}",
                                messageId: StdInfo.InfoTextPreviewFirstClick))
                            {
                                await Task.Delay(TimeSpan.FromSeconds(0.5));
                            }
                        }
                        btn.IsVisible = false;
                        break;
                }
#if WINDOWS
                Window!.Title = $"Clicked: {(e.Item as GlyphButton)?.OPID}";
                await Task.Delay(TimeSpan.FromSeconds(1));
                Window!.Title = "Configuration Demo";
#endif
            }
        }
    }
}
