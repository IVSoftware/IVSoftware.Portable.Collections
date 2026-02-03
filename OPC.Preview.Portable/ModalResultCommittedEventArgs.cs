namespace OPC.Preview.Portable
{
    [Obsolete("Use IOPClickable")]
    public class ModalResultCommittedEventArgs : EventArgs
    {
        internal ModalResultCommittedEventArgs(object? sender, string? text, Enum? modalResult, bool endModal)
        {
            Sender = sender;
            TextId = text ?? string.Empty;
            Result = modalResult ?? ModalResult.None;
            EndModal = endModal;
        }
        public object? Sender { get; }
        public string TextId { get; } = string.Empty;
        public Enum Result { get;  } = ModalResult.None;

        public bool EndModal { get; }

        public static void SetModalResult(object? sender, string textId, bool endModal = true)
        {
            ModalResultCommitted?.Invoke(
                sender: nameof(SetModalResult),
                new ModalResultCommittedEventArgs(
                    sender: sender, 
                    text: textId, 
                    modalResult: null,
                    endModal: endModal));
        }

        internal static void SetModalResult(object? sender, Enum modalResult, bool endModal = true)
        {
            ModalResultCommitted?.Invoke(
                sender: nameof(SetModalResult), 
                new ModalResultCommittedEventArgs(
                    sender: sender, 
                    text: null,
                    modalResult: modalResult,
                    endModal: endModal));
        }

        internal static void SetModalResult(object? sender, string textId, Enum? modalResult, bool endModal = true)
        {
            ModalResultCommitted?.Invoke(
                sender: nameof(SetModalResult),
                new ModalResultCommittedEventArgs(
                    sender: sender, 
                    text: textId, 
                    modalResult: modalResult,
                    endModal: endModal));
        }
        public static event EventHandler<ModalResultCommittedEventArgs>? ModalResultCommitted;
    }
    public static class ModalResultExtensions
    {
        public static void SetModalResult(this object? sender, string textId, bool endModal = true)
        {
            ModalResultCommittedEventArgs.SetModalResult(sender, textId, endModal);
        }
        public static void SetModalResult(this object? sender, Enum modalResult, bool endModal = true)
        {
            ModalResultCommittedEventArgs.SetModalResult(sender, modalResult, endModal);
        }
        public static void SetModalResult(this object? sender, string textId, Enum? modalResult, bool endModal = true)
        {
            ModalResultCommittedEventArgs.SetModalResult(sender, textId, modalResult, endModal);
        }
    }
}
