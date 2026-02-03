using IVSoftware.Portable.SQLiteMarkdown;

namespace IVSoftware.Portable.Collections.Common
{
    internal class MarkdownContextInternal<T> : MarkdownContext<T>
    {
        public void SetProtectedSearchState(SearchEntryState searchState)
        {
            SearchEntryState = searchState;
        }
        public void SetProtectedFilteringState(FilteringState filteringState)
        {
            FilteringState = filteringState;
        }
    }
}
