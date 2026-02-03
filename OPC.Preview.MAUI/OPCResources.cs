using System;
using System.Collections.Generic;
using System.Text;

namespace OPC.Preview.Maui
{
    public static class OPCResources
    {
        public static ResourceDictionary Styles => new()
        {
            MergedDictionaries =
            {
                new ResourceDictionary
                {
                    Source = new Uri(
                        "resource://OPC.Preview.Maui/Resources/Styles/Styles.xaml",
                        UriKind.Absolute)
                }
            }
        };
    }
}
