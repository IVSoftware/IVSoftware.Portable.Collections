using Newtonsoft.Json;
using OPC.Preview.Maui.Controls;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace OPC.Preview.Maui.Views;

public partial class StringView : ModalItemBaseView
{
	public StringView() => InitializeComponent();

    private void OnClicked(object sender, EventArgs e)
    {

    }

#if false && SAVE
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        var builder = new List<string>();
        var gb = this.GlyphButton;

        foreach (var pi in typeof(GlyphButton).GetProperties(
                     BindingFlags.Instance | BindingFlags.Public))
        {
            // Skip indexers
            if (pi.GetIndexParameters().Length > 0)
                continue;

            // Skip known graph / native bridges
            if (pi.Name is
                "Handler" or
                "Parent" or
                "BindingContext" or
                "Resources" or
                "Style")
                continue;

            object? value;
            try
            {
                value = pi.GetValue(gb);
            }
            catch (Exception ex)
            {
                value = $"<throws {ex.GetType().Name}>";
            }

            builder.Add($"{pi.Name} = {FormatValue(value)}");
        }

        var json = JsonConvert.SerializeObject(builder, Formatting.Indented);

#if DEBUG
        Debug.WriteLine($"GlyphButton dump:{Environment.NewLine}{json}");
#endif

        { }
    }

    static string? FormatValue(object? value)
    {
        if (value is null)
            return "null";

        return value switch
        {
            string s => $"\"{s}\"",
            Color c => c.ToArgbHex(),
            Thickness t => t.ToString(),
            Enum e => e.ToString(),
            _ => value.ToString() ?? "<null ToString>"
        };
    }
#endif
}