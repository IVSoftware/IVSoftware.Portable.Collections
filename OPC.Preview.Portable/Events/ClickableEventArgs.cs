using IVSoftware.Portable;
using IVSoftware.Portable.Collections.Lists;
using IVSoftware.Portable.Common.Exceptions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
namespace OPC.Preview.Portable.Events
{
    [DebuggerDisplay("{EventType} Event")]
    public class ClickableEventArgs
        : EventArgs
        , IOPMappable
    {
        /// <summary>
        /// Simple use where sender of event delegate is unambiguous.
        /// </summary>
        public ClickableEventArgs(ClickableEventType eventType)
            : this(null, Empty, eventType) { }

        /// <summary>
        /// Forward original native (or nested) EventArgs. Sender of event delegate must be unambiguous.
        /// </summary>
        public ClickableEventArgs(EventArgs eSender, ClickableEventType eventType)
            : this(null, eSender, eventType) { }

        /// <summary>
        /// Forward original native EventArgs while disambiguating (e.g. nested) sender.
        /// </summary>
        public ClickableEventArgs(object? sender, ClickableEventType eventType)
            : this(sender, Empty, eventType) { }

        /// <summary>
        /// Clone e.g. from nested child event.
        /// </summary>
        public ClickableEventArgs(object? sender, ClickableEventArgs e)
            : this(sender, e, e.EventType) { }

        [Canonical]
        public ClickableEventArgs(object? sender, EventArgs eSender, ClickableEventType eventType)
        {
            NativeEventArgs = eSender;
            EventType = eventType;
            Sender = sender;
            if (sender is IOPMappable opm)
            {
                OPID = opm.OPID;
            }
        }

        /// <summary>
        /// Document original sender if different from the event delegate sender.
        /// </summary>
        /// <remarks>
        /// For example, when a container like CommandBar forwards a child click.
        /// </remarks>
        public object? Sender { get; }

        public ClickableEventType EventType { get; }

        /// <summary>
        /// Monotonic handling state synchronized across nested ClickableEventArgs.
        /// </summary>
        public bool Handled
        {
            get
            {
                if(_handled)
                {
                    return true;
                }
                else
                {
                    return (NativeEventArgs as ClickableEventArgs)?.Handled == true;
                }
            }
            set
            {
                _handled = value;
                if (NativeEventArgs is ClickableEventArgs eChild)
                {
                    eChild.Handled = value;
                }
            }
        }
        bool _handled = false;


        /// <summary>
        /// The original platform event raised by
        /// the  UI surface (this is often Empty).
        /// </summary>
        public EventArgs NativeEventArgs { get; }

        /// <summary>
        /// Document distinct nodes in order as ancestors are walked.
        /// </summary>
        public ObservablePreviewCollection<IOPClickableSink> Visited { get; } = new();

        /// <summary>
        /// This event can be reconfigured as awaitable by the handler.
        /// </summary>
        public TaskAwaiter GetAwaiter()
        {
            if (TCS is not null)
            {
                return TCS.Task.GetAwaiter();
            }
            return Task.CompletedTask.GetAwaiter();
        }
        public TaskCompletionSource? TCS
        {
            get => _tcs;
            set
            {
                if (_tcs is null)
                {
                    _tcs = value;
                }
                else
                {
                    this.ThrowSoft<InvalidOperationException>($"{nameof(ClickableEventArgs)} already has a TCS.");
                }
            }
        }

        /// <summary>
        /// Monotonic state channel for Handled.
        /// </summary>
        public Enum OPID
        {
            get
            {
                if (NativeEventArgs is ClickableEventArgs eChild)
                {
                    return eChild.OPID;
                }
                else
                {
                    return _opid;
                }
            }
            set => _opid = value;
        }
        Enum _opid = OPReserved.DefaultId;


        TaskCompletionSource? _tcs = default;
    }
}
