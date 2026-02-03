using IVSoftware.Portable;
using IVSoftware.Portable.SQLiteMarkdown;
using IVSoftware.Portable.Xml.Linq;
using OPC.Preview.Portable.Events;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Input;
using static IVSoftware.Portable.GlyphProvider;

namespace OPC.Preview.Portable
{
    public class PortableBindingContext
        : INotifyPropertyChanged
        , IOPClickableSink
    {
        [Probationary]
        protected virtual void PushModal(Type config) => PushModal([config]);

        [Probationary]
        protected virtual void PushModal(Type[] multiConfig) { }

        [Canonical]
        protected virtual void PushModalOPID(Enum opid)
        {
            ModalStack.Push(opid);
        }

        protected readonly TolerantModalStack ModalStack = new();
        protected class TolerantModalStack : Stack<Enum>
        {
            public new Enum? Peek() =>
                Count == 0 
                ? null 
                : base.Peek();
        }

        /// <summary>
        /// Tolerant pop.
        /// </summary>
        protected virtual (object? sender, Enum result)? PopModalOPID(Enum result)
        {
            if (ModalStack.Count == 0)
            {
                Debug.Fail($@"ADVISORY - Benign but unexpected.");
                return null;
            }
            else
            {
                return (sender: ModalStack.Pop(), result: result);
            }
        }

        public Enum? AppThemePCL
        {
            get => _appTheme;
            set
            {
                if (!Equals(_appTheme, value))
                {
                    _appTheme = value;
                    OnPropertyChanged();
                }
            }
        }
        Enum? _appTheme = null;


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, this))
            {
                PropertyChanged?.Invoke(sender, e);
            }
        }

        public virtual async Task SinkClickableEvent(object sender, ClickableEventArgs e) { }

        public Type CommandBarConfig
        {
            get => _commandBarConfig;
            set
            {
                if (!Equals(_commandBarConfig, value))
                {
                    _commandBarConfig = value;
                    OnPropertyChanged();
                }
            }
        }
        Type _commandBarConfig = typeof(EditingCommands);


        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
