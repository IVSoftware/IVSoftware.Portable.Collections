#if ANDROID
using Android.Views;
#endif

namespace OPC.Preview.Maui
{
    public static class Extensions
    {
        public static MauiAppBuilder UseOPC(this MauiAppBuilder builder)
        {
            builder.ConfigureFonts(fonts =>
            {
                fonts.AddFont("icon-basics.ttf", "icon-basics");
                fonts.AddFont("icon-radio.ttf", "icon-radio");
            });

            return builder;
        }
    }
}
