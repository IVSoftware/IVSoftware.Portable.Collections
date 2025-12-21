using System.Collections;
using System.Collections.Specialized;
using System.Xml.Linq;

namespace IVSoftware.Portable.Collections.Dictionaries
{
    /// <summary>
    /// Dictionary indexer class that employs jagged multi-term indexer overloads to return scoped dictionaries on demand.
    /// </summary>
    /// <remarks>
    /// The native IDictionary key provides O(1) access to hierarchy metadata, with an optional
    /// second O(1) hop to resolve any model node. This allows one IDictionary to be effectively
    /// parented by another without requiring explicit cooperation from the underlying dictionary.
    ///
    /// USAGE
    /// - Ad hoc object-object dictionaries
    ///   Example: IDictionary dunk = brisk[typeof(Type), typeof(PropertyInfo)]
    ///   
    /// - Ad hoc object-object dictionary values
    ///   Example: PropertyInfo pi = brisk[typeof(Type), typeof(PropertyInfo)].Get<PropertyInfo>(nameof(Button.IsVisible))
    /// 
    /// - Strongly typed dictionaries with preregistration
    ///   Example: PropertyInfo pi = brisk[typeof(Button), typeof(PropertyInfo)][nameof(Button.IsVisible)]
    /// </remarks>
    [UnilateralContract(activateAs: typeof(BriskDictionary))]
    public interface IBriskDictionary
        : IDictionary
        , INotifyCollectionChanged
        , IInsistent
    {
        /// <summary>
        /// Hierarchal dictionary bindings.
        /// </summary>
        XElement Model { get; }

        /// <summary>
        /// Redirects the indexer after formalizing the jagged keys.
        /// </summary>
        [Indexer]
        IObservableDictionary this[object key, params object[] moreKeys] { get; }

        /// <summary>
        /// Redirects the method after formalizing the jagged keys.
        /// </summary>
        bool ContainsKey(object key1, params object[] keysN);

        /// <summary>
        /// Produces the current model with all nested dictionaries expanded as an XML string.
        /// </summary>
        string ViewExpandedModel();

        /// <summary>
        /// Allows custom formatting of xkey node during view expansion.
        /// </summary>
        /// <remarks>
        /// This event also has a static version <see cref="AnyExpandXKeyFormatRequested"/>
        /// </remarks>
        event EventHandler<ExpandXKeyFormatRequestedEventArgs>? ExpandXKeyFormatRequested;
    }
}
