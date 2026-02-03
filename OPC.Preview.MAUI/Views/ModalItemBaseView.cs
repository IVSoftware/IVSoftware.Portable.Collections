using OPC.Preview.Maui.Controls;
using OPC.Preview.Portable;
using System.Windows.Input;

namespace OPC.Preview.Maui.Views;

public abstract class ModalItemBaseView : ContentView
{
    private void OnClicked(object sender, EventArgs e)
    {
        if (sender is GlyphButton button)
        {
           sender.SetModalResult(textId: button.Text, modalResult: button.OPID);
        }
    }
}