using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OPC.Preview.Maui.Models;
using System.Diagnostics;

namespace QueryFilterList.Maui.Demo
{
    partial class MainPage
    {
        private void OnCollectionViewHandlerChanged(object sender, EventArgs e)
        {
#if false
            if (sender is CollectionView cv && cv.Handler?.PlatformView is ListViewBase lv)
            {
                lv.AddHandler(
                    UIElement.PointerReleasedEvent,
                    new PointerEventHandler(OnPointerReleased),
                    handledEventsToo: true);
            }

            void OnPointerReleased(object sender, PointerRoutedEventArgs e)
            {
                ItemCardModel? model = null;
                if (BindingContext.SelectionContext.PressedItem is { } item)
                {
                    BindingContext.SelectionContext.ItemRelease();
                    model = item;
                }
            }
#endif
        }
    }
}
