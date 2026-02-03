using IVSoftware.Portable;
using IVSoftware.Portable.Collections;

namespace OPC.Preview.Portable
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InfoTextAttribute : Attribute
    {
        public InfoTextAttribute(string message)
        {
            Message = message.Trim();
        }
        public string Message { get; }
    }

    public enum GroupBoxItemStyle
    {
        /// <summary>
        /// Action (no state) with text only
        /// </summary>
        String,

        /// <summary>
        /// Action (no state) with glyph only
        /// </summary>
        Glyph,

        /// <summary>
        /// Action (no state) with glyph + text
        /// </summary>
        GlyphString,

        /// <summary>
        /// Multi state where glyph defaults to check box.
        /// </summary>
        CheckBox,

        /// <summary>
        /// One hot where glyph defaults to radio.
        /// </summary>
        Radio,
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class GroupAttribute : Attribute
    {
        public GroupAttribute(GroupBoxItemStyle style = GroupBoxItemStyle.String)
        : this(null, style) { }

        [Canonical]
        public GroupAttribute(string? name, GroupBoxItemStyle style = GroupBoxItemStyle.String)
        {
            Name = name;
            Style = style;
        }

        public string? Name { get; set; }
        public GroupBoxItemStyle Style { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class EditorTemplateAttribute : Attribute
    {
        public EditorTemplateAttribute(Type template)
        { 
            Template = template;
        }
        public Type Template { get; set; }
    }
}
