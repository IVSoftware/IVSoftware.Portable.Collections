using OPC.Preview.Maui.Models;
using OPC.Preview.Portable;

namespace OPC.Preview.Maui.Converters
{
    public sealed class InfoTextRowTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HeaderTemplate { get; init; } = null!;
        public DataTemplate BulletTemplate { get; init; } = null!;
        public DataTemplate TextTemplate { get; init; } = null!;
        public DataTemplate SeparatorTemplate { get; init; } = null!;

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var row = (InfoOverlayRow)item;
            return row.Style switch
            {
                InfoOverlayRowStyle.Header => HeaderTemplate,
                InfoOverlayRowStyle.BulletText => BulletTemplate,
                InfoOverlayRowStyle.Separator => SeparatorTemplate,
                _ => TextTemplate
            };
        }
    }
}
