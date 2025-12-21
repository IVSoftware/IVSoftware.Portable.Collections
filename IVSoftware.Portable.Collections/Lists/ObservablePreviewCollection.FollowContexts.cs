using IVSoftware.Portable.Collections.Common;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Collections.FollowContexts;
using System.Collections;
using System.Reflection;

namespace IVSoftware.Portable.Collections.Lists
{

    partial class ObservablePreviewCollection<T>
    {
        private void InitializeFollowContexts()
        {
            foreach (var pi in typeof(T).GetProperties())
            {
                if(pi?.GetCustomAttributes<FollowAttribute>().SingleOrDefault() is { } attr)
                {
                    FollowContexts[pi.Name] = new FollowContext<T>(this, pi.Name);
                }
            }
        }
        public FollowDictionary<T> FollowContexts { get; } = new();
    }

    public class FollowDictionary<T> : TolerantDictionary<string, FollowContext<T>>
    {
        public void Follow(IObservablePreviewCollection owner, string binding) => this[binding] = new FollowContext<T>(owner, binding);
        public void Follow(FollowContext<T> context) => this[context.PropertyInfo.Name] = context;
    }
}
