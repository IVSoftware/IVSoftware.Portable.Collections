using IVSoftware.Portable.Common.Exceptions;
using System.Collections;

namespace IVSoftware.Portable.Collections.Common
{
    public class CollectionRange
    {
        public CollectionRange(int startindex, int endIndex)
        {
            StartIndex = Math.Min(startindex, endIndex);
            EndIndex = Math.Max(startindex, endIndex);
        }
        public int StartIndex { get; }
        public int EndIndex
        {
            get => _endIndex;
            set
            {
                if (!Equals(_endIndex, value))
                {
                    if (value < StartIndex)
                    {
                        this.ThrowHard<IndexOutOfRangeException>($"{nameof(EndIndex)}={value} cannot be less than {nameof(StartIndex)}={StartIndex}");
                        return;
                    }
                    else
                    {
                        _endIndex = value;
                        Count = 1 + (_endIndex - StartIndex);
                    }
                }
            }
        }
        int _endIndex = default;

        public int Count
        {
            get => _count;
            set
            {
                if (!Equals(_count, value))
                {
                    _count = value;
                    EndIndex = StartIndex + (_count - 1);
                }
            }
        }
        int _count = default;
    }
}
