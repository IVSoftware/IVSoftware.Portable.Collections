using OPC.Preview.Maui.Views;
using OPC.Preview.Portable.Models;
using System.Windows.Input;

namespace OPC.Preview.Maui.Converters
{
    public class ModalTemplateSelector : DataTemplateSelector
    {
        private DataTemplate StringViewTemplate { get; } = new(typeof(StringView));
        private DataTemplate GlyphButtonViewTemplate { get; } = new(typeof(GlyphButtonView));
        private DataTemplate GroupBoxViewTemplate { get; } = new(typeof(GroupBoxView));
        private DataTemplate PropertyViewTemplate { get; } = new(typeof(PropertyView));
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            switch (item)
            {
                default:
                case string: return StringViewTemplate;
                case GlyphButtonModel: return GlyphButtonViewTemplate;
                case GroupBoxModel: return GroupBoxViewTemplate;
                case PropertyInfoModel: return PropertyViewTemplate;
            }
        }
    }
}
