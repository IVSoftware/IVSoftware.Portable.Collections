using Microsoft.Extensions.DependencyInjection;

namespace FilteredList.Maui.Demo
{
    public partial class App : Application
    {
        public App() => InitializeComponent();

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.Created += async (_, _) =>
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    nativeWindow.Activate();

                    IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                    var winId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

                    var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                        winId,
                        Microsoft.UI.Windowing.DisplayAreaFallback.Primary
                    );

                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(winId);
                    appWindow.Resize(new(540, 960));
                    appWindow.Move(new(
                        displayArea.WorkArea.X + (displayArea.WorkArea.Width - 540) / 2,
                        displayArea.WorkArea.Y + (displayArea.WorkArea.Height - 960) / 2
                    ));
                }
            };
#endif

            return window;
        }
    }
}