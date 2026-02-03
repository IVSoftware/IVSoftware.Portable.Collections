
#if false
// Use IVSoftware.Portable.Xml.Linq.CommandPCL

using System.Windows.Input;

namespace OPC.Preview.Portable
{
    public class CommandPCL : ICommand
    {
        private readonly Func<object?, bool>? _canExecute;
        private readonly Action<object?> _execute;
        private readonly WeakEventManager _weakEventManager = new WeakEventManager();

        public CommandPCL(Action<object?> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public CommandPCL(Action execute) : this(o => execute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
        }

        public CommandPCL(Action<object?> execute, Func<object?, bool> canExecute) : this(execute)
        {
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public CommandPCL(Action execute, Func<bool> canExecute) : this(o => execute(), o => canExecute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { _weakEventManager.AddEventHandler(value); }
            remove { _weakEventManager.RemoveEventHandler(value); }
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void ChangeCanExecute()
        {
            _weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(CanExecuteChanged));
        }
    }
    public class CommandPCL<T> : ICommand
    {
        private readonly Func<T?, bool>? _canExecute;
        private readonly Action<T?> _execute;
        private readonly WeakEventManager _weakEventManager = new WeakEventManager();

        public CommandPCL(Action<T?> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public CommandPCL(Action execute) : this(o => execute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
        }

        public CommandPCL(Action<T?> execute, Func<T?, bool> canExecute) : this(execute)
        {
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public CommandPCL(Action execute, Func<bool> canExecute) : this(o => execute(), o => canExecute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));
        }

        public bool CanExecute(T? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { _weakEventManager.AddEventHandler(value); }
            remove { _weakEventManager.RemoveEventHandler(value); }
        }

        public void Execute(T? parameter)
        {
            _execute(parameter);
        }

        public void ChangeCanExecute()
        {
            _weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(CanExecuteChanged));
        }
        bool ICommand.CanExecute(object? parameter)
            => parameter is T t
                ? CanExecute(t)
                : parameter == null && default(T) == null
                    ? CanExecute(default)
                    : false;

        void ICommand.Execute(object? parameter)
        {
            if (parameter is T t) Execute(t);
            else if (parameter == null && default(T) == null) Execute(default);
            else throw new InvalidOperationException(
                $"Expecting parameter type: {typeof(T).Name} got parameter type {parameter?.GetType()}");
        }
    }

    public class WeakEventManager
    {
        private List<WeakReference<EventHandler?>> _eventHandlers = new List<WeakReference<EventHandler?>>();

        public void AddEventHandler(EventHandler? handler)
        {
            if (handler != null)
            {
                _eventHandlers.Add(new WeakReference<EventHandler?>(handler));
            }
        }

        public void RemoveEventHandler(EventHandler handler)
        {
            _eventHandlers.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out EventHandler? target))
                    return target == handler;
                return false;
            });
        }

        public void HandleEvent(object sender, EventArgs e, string eventName)
        {
            _eventHandlers.RemoveAll(wr => !wr.TryGetTarget(out EventHandler? _));
            foreach (var weakReference in _eventHandlers)
            {
                if (weakReference.TryGetTarget(out EventHandler? handler))
                {
                    handler?.Invoke(sender, e);
                }
            }
        }
    }
}
#endif