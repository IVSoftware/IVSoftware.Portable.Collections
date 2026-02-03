using System.ComponentModel;

namespace OPC.Preview.Portable.Models
{
    public abstract class ItemCardModelEditorConfiguration
    {
        public string? Description { get; }

        [Description("Keywords")]
        public string? KeywordsDisplay { get; }
        public string? Tags { get; }
    }
}
