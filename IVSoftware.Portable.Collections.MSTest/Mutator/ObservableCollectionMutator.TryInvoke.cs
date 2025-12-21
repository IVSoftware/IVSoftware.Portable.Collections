using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using static IVSoftware.Portable.Collections.Framework;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{
    public partial class ObservableCollectionMutator
    {

        /// <summary>
        /// IList.Move does not exist.
        /// </summary>
        internal static void InvokeMove(IList list, int oldStartingIndex, int newStartingIndex)
        {
            var dunk = Brisk[list.GetType(), typeof(MethodInfo)];

            var move =
                Brisk[list.GetType(), typeof(MethodInfo)][OCMCall.Move]
                    .SafeAs<MethodInfo>();

            move!.Invoke(list, [oldStartingIndex, newStartingIndex]);
        }

        /// <summary>
        /// IList.Remove does not return bool and we need to call on generic type.
        /// </summary>
        internal static bool InvokeRemove(IList list, object? item)
        {
            var dunk = Brisk[list.GetType(), typeof(MethodInfo)];

            var remove =
                Brisk[list.GetType(), typeof(MethodInfo)][OCMCall.Remove]
                    .SafeAs<MethodInfo>();

            return (bool)remove!.Invoke(list, [item])!;
        }
    }
}
