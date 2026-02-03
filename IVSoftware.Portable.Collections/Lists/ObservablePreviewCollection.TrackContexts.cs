using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.TrackingContexts;
using System.Collections;
using System.Reflection;

namespace IVSoftware.Portable.Collections.Lists
{

    partial class ObservablePreviewCollection<T>
    {
        /// <summary>
        /// Enumerate public properties, adding TrackContexts for decorated properties.
        /// </summary>
        private void InitializeTrackContexts()
        {
            foreach (var pi in typeof(T).GetProperties())
            {
                if(pi?.GetCustomAttributes<TrackAttribute>().SingleOrDefault() is { } attr)
                {
                    TrackContexts[pi.Name] = new TrackContext<T>(this, pi.Name);
                }
            }
        }
        public TrackDictionary<T> TrackContexts { get; } = new();
    }

    public class TrackDictionary<T> : TolerantDictionary<string, TrackContext<T>>
    {
        public void Track(IObservablePreviewCollection owner, string binding) => this[binding] = new TrackContext<T>(owner, binding);
        public void Track(TrackContext<T> context) => this[context.PropertyInfo.Name] = context;
    }
}
