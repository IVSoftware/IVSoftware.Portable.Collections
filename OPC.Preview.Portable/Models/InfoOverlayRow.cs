using OPC.Preview.Portable;
using System;
using System.Collections.Generic;
using System.Text;

namespace OPC.Preview.Maui.Models
{
    public sealed class InfoOverlayRow
    {
        public InfoOverlayRowStyle Style { get; init; }
        public string Text { get; init; } = string.Empty;
    }
}
