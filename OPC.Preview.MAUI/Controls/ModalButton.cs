using OPC.Preview.Portable;

namespace OPC.Preview.Maui.Controls
{
    public class ModalButton : ButtonBase
    {
        public ModalButton() 
        {
            Clicked += (sender, e) => this.SetModalResult(Text);
        }
    }
}
