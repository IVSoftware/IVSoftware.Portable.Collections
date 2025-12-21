using System.Collections;

namespace IVSoftware.Portable.Collections
{
    public interface IRangeable
    {
        void AddRange(IEnumerable items);
        int AddRangeDistinct(IEnumerable items);
        void InsertRange(int startingIndex, IEnumerable items);
        void RemoveRange(int startingIndex, int endingIndex);
        int RemoveMultiple(IEnumerable items);
    }
    public interface IRangeable<T> : IRangeable
    {
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Addin multiple items that are individually validated as distinct..
        /// </summary>
        int AddRangeDistinct(IEnumerable<T> items);

        /// <summary>
        /// Removal of a multiple contiguous items.
        /// </summary>
        void InsertRange(int startingIndex, IEnumerable<T> newItems);

        /// <summary>
        /// Removal of a multiple items that aren't necessarily contiguous.
        /// </summary>
        int RemoveMultiple(IEnumerable<T> items);
    }
}
