using IVSoftware.Portable;
using IVSoftware.Portable.Collections.Dictionaries;
using OPC.Preview.Portable.Events;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace OPC.Preview.Portable
{
    /// <summary>
    /// Represents an object that accepts a single or default configuration.
    /// </summary>
    public interface IOPConfigurable
    {
        /// <summary>
        /// Single configuration or null for None.
        /// </summary>
        Type? Configuration { get; }

        /// <summary>
        /// - Typical scenarios call for T : Enum
        /// - Another use case would be an IConfigurable property page for T
        /// - This is the rationale for leaving T unconstrained.
        /// </summary>
        void Configure<T>();

        /// <summary>
        /// Imperative configuration.
        /// </summary>
        /// <remarks>
        /// Alternatively, a common scenario is to bind the Configuration property.
        /// </remarks>
        void Configure(Type? type);
    }

    /// <summary>
    /// Extends IConfigurable to allow single or multiple configurations.
    /// </summary>
    /// <remarks>
    /// Example - ConfigurableCollectionView
    /// A single configuration typeof(EditingCommands) might display:
    /// - Add
    /// - Edit
    /// - Delete
    /// A multi configuration might display two group boxes
    /// - typeof(SetCheckOptions) 
    /// -- CheckAll
    /// -- UncheckAll
    /// - typeof(ShowCheckOptions)
    /// -- ShowAll
    /// -- Show Unchecked
    /// -- Show Checked
    /// </remarks>
    public interface IMultiConfigurable : IOPConfigurable
    {
        Type[] MultiConfiguration { get; }

        /// <summary>
        /// Set multiple configuration types.
        /// </summary>
        /// <remarks>
        /// - In C# overload resolution, a non-params method is always preferred
        ///   over a params method when both are otherwise applicable.
        /// - Consider unrolling in case client mixes Type with IEnumerable of Type.
        /// - Alternatively, a common scenario is to bind the MultiConfiguration property.
        /// </remarks>
        void Configure(params Type[] types);
    }

    /// <summary>
    /// A settings dictionary that can be indexed either by a raw 
    /// string or by a StdEnum (ToString) that standardizes one.
    /// </summary>
    public interface ISettingsSource : IDictionary, INotifyPropertyChanged
    {
        object? this[Enum key] { get; set; }
        object? this[string key] { get; set; }
    }
    public interface IOPSettingsSink
    {
        ISettingsSource? Settings { get; }
    }
    public interface IInfoOverlay
    { }

    public interface IOPMappable
    {
        Enum OPID { get;  }
    }

    public interface IOPClickable
    {
        /// <summary>
        /// Raises the events for this control.
        /// </summary>
        /// <remarks>
        /// Typical use cases include the ability to walk 
        /// peers and ancestors in search of IOPClickableSink.
        /// This walk can be awaited at each node.
        /// </remarks>
        Task PerformClickableEvent(object sender, ClickableEventArgs e);

        event EventHandler? Clicked;
        event EventHandler? Pressed;
        event EventHandler? LongPressed;
        event EventHandler? Released;
        ICommand ClickableEventCommand { get; }
    }

    public enum ClickableEventType
    {
        Pressed,
        Clicked,
        LongPressed,
        Released,
    }

    /// <summary>
    /// Intercept or react to click events bubbling up from XML-bound OnePageClickable descendants.
    /// </summary>
    public interface IOPClickableSink
    {
        /// <summary>
        /// Awaitable handler
        /// </summary>
        Task SinkClickableEvent(object sender, ClickableEventArgs e);
    }

    public interface IOPCommandBar
    : IOPConfigurable
    , IOPClickable
    {
        LayoutOptionFlag LayoutOptions { get; }
    }

    public interface IOPItemEditor
    {
        /// <summary>
        /// This can be a Type for a new item, or an instance for an existing item.
        /// </summary>
        object Item { get; set; }
    }
}
