using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{
    public enum MutationPhase
    {
        Expected,
        Actual,
    }

    public enum ValidityLottery
    {
        /// <summary>
        /// Generate valid data
        /// </summary>
        Valid,

        /// <summary>
        /// Generate an error that throws before preview.
        /// </summary>
        Preemptive,

        /// <summary>
        /// Induce an error in the preview handler. 
        /// </summary>
        /// <remarks>
        /// In other words, anticipate an initial call that is legal, 
        /// then deliberately corrupt the CollectionChanging flow 
        /// e.g coerce index, type, action etc. on the test side.
        /// </remarks>
        Responsive,
    }

    [Flags]
    public enum ErrorInjectFlag
    {
        /// <summary>
        /// Far and away the most likely error.
        /// </summary>
        NewStartingIndex = 0x01,

        NewItems = NewStartingIndex << 1,
        OldStartingIndex = NewItems << 1,
        OldItems = OldStartingIndex << 1,
        Action = OldItems << 1,
    }
}
