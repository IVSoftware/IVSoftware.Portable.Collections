using System.Windows.Input;

namespace IVSoftware.Portable.Collections
{
    /// <summary>
    /// A model that maintains callable access to its container.
    /// </summary>
    /// <remarks>
    /// Subclassed observable collections may implement this interface 
    /// to maintain container context that might otherwise be lost when
    /// item views are realized through data templates or selectors.
    /// </remarks>
    public interface IContainerBindingContext
    {
        object? ContainerBindingContext { get; set; }
    }
}
