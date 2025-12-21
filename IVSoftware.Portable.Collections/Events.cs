using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using IVSoftware.Portable.Common.Exceptions;

namespace IVSoftware.Portable.Collections
{

    /// <summary>
    /// Event arguments raised when a property getter resolves to a default 
    /// value for a non-nullable type, indicating an invalid or unintended response.
    /// </summary>
    public class InvalidGetEventArgs : InvalidOperationException
    {
        public InvalidGetEventArgs(Type requestedType, string? propertyName)
        {
            RequestedType = requestedType;
            PropertyName = propertyName ?? string.Empty;
        }

        /// <summary>
        /// The type that was requested by the caller, e.g. typeof(int).
        /// </summary>
        public Type RequestedType { get; }

        /// <summary>
        /// The property name associated with the failed lookup.
        /// </summary>
        public string PropertyName { get; }

        public override string ToString()
            => $"InvalidResponse: {PropertyName} as {RequestedType.Name}";
    }

    public class ExpandXKeyFormatRequestedEventArgs
    {
        public ExpandXKeyFormatRequestedEventArgs(XElement xkey, object? valueToFormat)
        {
            XKey = xkey;
            ValueToFormat = valueToFormat;
        }
        /// <summary>
        /// Contents may be freely customized.
        /// </summary>
        public XElement XKey { get; }

        public object? ValueToFormat { get; }
    }
}