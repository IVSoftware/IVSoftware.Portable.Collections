using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OPC.Preview.Portable.Models
{
    public enum Placement
    {
        /// <summary>
        /// Ignore Text
        /// </summary>
        Glyph,

        /// <summary>
        /// Ignore Glyph
        /// </summary>
        Text,

        /// <summary>
        /// Render glyph before text in the primary flow direction.
        /// </summary>
        GlyphBeforeText,

        /// <summary>
        /// Render glyph above text.
        /// </summary>
        GlyphAboveText,
    }
    public class GlyphButtonModel : ModalItemBaseModel
    {
        public GlyphButtonModel(Enum? stdIconName)
        {
            OPID = stdIconName;
        }
        public string Text
        {
            get => _text;
            set
            {
                if (!Equals(_text, value))
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }
        string _text = string.Empty;
        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                if (!Equals(_fontFamily, value))
                {
                    _fontFamily = value;
                    OnPropertyChanged();
                }
            }
        }
        string _fontFamily = string.Empty;

        public string Glyph
        {
            get => _glyph;
            set
            {
                if (!Equals(_glyph, value))
                {
                    _glyph = value;
                    OnPropertyChanged();
                }
            }
        }
        string _glyph = string.Empty;

        /// <summary> 
        /// An enum member with a [Glyph] attribute can
        /// potentially overwrite both FontFamily and Glyph. 
        ///</summary>
        public Enum? OPID
        {
            get => _opid;
            set
            {
                if (!Equals(_opid, value))
                {
                    _opid = value;
                    OnPropertyChanged();
                }
            }
        }
        Enum? _opid = default;

        public Placement Placement
        {
            get => _placement;
            set
            {
                if (!Equals(_placement, value))
                {
                    _placement = value;
                    OnPropertyChanged();
                }
            }
        }
        Placement _placement = default;
    }
}