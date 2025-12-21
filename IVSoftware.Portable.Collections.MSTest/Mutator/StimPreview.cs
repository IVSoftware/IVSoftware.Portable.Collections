using IVSoftware.Portable.Collections.MSTest.TestUtils;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Text.Json.Serialization;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{
    /// <summary>
    /// Class for aspirational and pathological stims.
    /// </summary>
    public class StimPreview
    {
        public StimPreview(OCMCall ocmCall)
        {
            Action = ocmCall.ToNotifyCollectionChangingAction();
        }
        public StimPreview(NotifyCollectionChangingAction action)
        {
            Action = action;
        }
        // CTor used for tmp valid for copying the tmp to Valid
        public StimPreview(StimPreview? other = null)
        {
            if (other is not null)
            {
                Action = other.Action;
                NewStartingIndex = other.NewStartingIndex;
                OldStartingIndex = other.OldStartingIndex;
                NewItems = other.NewItems;
                OldItems = other.OldItems;
            }
        }

        // Construct invalid.
        public StimPreview(StimPreview other, IList? errorSource)
        {
            Action = other.Action;
            NewItems = new List<object?>(["New Item Error"]);
            OldItems = new List<object?>(["Old Item Error"]);
            RefreshErrorSource(errorSource);
        }
        public static object? @void { get; } = null;

        [JsonConverter(typeof(StringEnumConverter))]
        public NotifyCollectionChangingAction Action { get; internal set; }
        public int NewStartingIndex { get; internal set; } = -1;
        public int OldStartingIndex { get; internal set; } = -1;
        public IList? NewItems { get; internal set; }
        public IList? OldItems { get; internal set; }

#if false
        public object? Result
        {
            get => _result;
            set
            {
                if (!Equals(_result, value))
                {
                    _result = value;
                }
            }
        }
        object? _result = @void;
#endif

        public object? GetNewItemSingle() => NewItems?.Cast<object?>().Single()!;

        public object? GetOldItemSingle() => OldItems?.Cast<object?>().Single()!;

        public void RefreshErrorSource(IList? errorSource)
        {
            if (errorSource is not null)
            {
                NewStartingIndex = errorSource.Count + 1;
                OldStartingIndex = errorSource.Count;
            }
        }
    }
}
