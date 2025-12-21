using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest.TestTargets
{
    /// <summary>
    /// For testing as abstract.
    /// </summary>
    interface ISimpleClass
    {
        int InstanceCount { get; }
    }

    /// <summary>
    /// For testing as UC.
    /// </summary>

    [UnilateralContract(activateAs: typeof(SimpleClass))]
    interface ISimpleClassUC
    {
        int InstanceCount { get; }
    }

    /// <summary>
    /// For testing as typeof(T).
    /// </summary>
    class SimpleClass 
        : ISimpleClass
        , ISimpleClassUC
        , INotifyPropertyChanged
    {
        public int InstanceCount => _instanceCount;
        static int _instanceCount = 0;

        public static void ResetInstanceCount() => _instanceCount = 0;
        public SimpleClass()
        {
            _instanceCount++;
        }
        public DateTimeOffset TimeStamp { get; set; } = DateTime.UnixEpoch;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
