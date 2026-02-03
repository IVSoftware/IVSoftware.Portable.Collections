using IVSoftware.Portable;
using OPC.Preview.Portable;
using OPC.Preview.Portable.Events;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OPC.Preview.Maui.Controls
{
    public class ButtonBase
        : Button
        , IOPMappable
        , IOPClickable
    {
        public ButtonBase()
        {
            Pressed += async (sender, _e) =>
            {
                await PerformClickableEvent(this, new ClickableEventArgs(this, _e, ClickableEventType.Pressed));
            };
            Clicked += async (sender, _e) =>
            {
                await PerformClickableEvent(this, new ClickableEventArgs(this, _e, ClickableEventType.Clicked));
            };
            Released += async(sender, _e) =>
            {
                await PerformClickableEvent(this, new ClickableEventArgs(this, _e, ClickableEventType.Released));
            };
        }

        // https://github.com/dotnet/maui/issues?q=is%3Aissue%20state%3Aopen%20pointerover%20visual%20state
        protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            switch (propertyName)
            {
                case nameof(IsVisible):
#if WINDOWS
                    // [Careful]
                    // This is a DEBILITATING CHANGE on any other platform
                    // because it suppresses button clicks COMPLETELY.
                    if (IsVisible)
                    {
                        VisualStateManager.GoToState(this, "Normal");
                        var pointerMove = new PointerGestureRecognizer();
                        pointerMove.PointerMoved += (sender, e) =>
                        {
                            GestureRecognizers.Remove(pointerMove);
                            VisualStateManager.GoToState(this, "PointerOver");
                        };
                        GestureRecognizers.Add(pointerMove);
                    }
#endif
                    break;
            }
        }
        public async Task PerformClickableEvent(object sender, ClickableEventArgs e)
        {
            switch (e.EventType)
            {
                case ClickableEventType.Pressed:
                    IsLongPressDetected = false;
                    WDTLongPressed.StartOrRestart();
                    break;
                case ClickableEventType.Released:
                    WDTLongPressed.Cancel();
                    break;
            }

            // Fire and await the event command.
            ClickableEventCommand?.Execute(e);
            await e;


            Element? current = null, parent; 
            foreach (var sink in _sinks)
            {
                await sink.SinkClickableEvent(sender, e);
                if(e.Handled)
                {
                    return;
                }
                else
                {
                    current = sink as Element;
                }
            }
            current ??= this;
            parent = current.Parent;
            while (parent != null)
            {
                if (parent is IOPClickableSink sink)
                {
                    _sinks.Add(sink);
                    await sink.SinkClickableEvent(sender, e);
                    if (e.Handled)
                    {
                        return;
                    }
                }
                parent = parent.Parent;
            }
        }

        private readonly List<IOPClickableSink> _sinks = new();


        public static readonly BindableProperty OPIDProperty =
            BindableProperty.Create(
                propertyName: nameof(OPID),
                returnType: typeof(Enum),
                declaringType: typeof(ButtonBase),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
#if ABSTRACT
                    // Don't double dip on this.
                    // The OPID property *already* assigns text
                    // from an opid with a glyph attribute.
                    if (bindable is ButtonBase @this && newValue is Enum opid)
                    {

                    }
#endif
                });

        public Enum OPID
        {
            get => (Enum)GetValue(OPIDProperty);
            set => SetValue(OPIDProperty, value);
        }

        public Enum? IconKey
        {
            get => _iconKey;
            set
            {
                if (!Equals(_iconKey, value))
                {
                    _iconKey = value;
                    OnPropertyChanged();
                }
            }
        }
        Enum? _iconKey = default;

        public WatchdogTimer WDTLongPressed
        {
            get
            {
                if (_wdtLongPressed is null)
                {
                    _wdtLongPressed = new WatchdogTimer
                    {
                        Interval = LongPressedDelay,
                    };
                    _wdtLongPressed.RanToCompletion += (sender, _) =>
                    {
                        IsLongPressDetected = true;
                        OnLongPressed();
                    };
                }
                return _wdtLongPressed;
            }
        }
        WatchdogTimer? _wdtLongPressed = null;

        public TimeSpan LongPressedDelay
        {
            get => _longPressedDelay;
            set
            {
                if (!Equals(_longPressedDelay, value)
                    && value.TotalSeconds > LONG_PRESSED_MIN_SECONDS)
                {
                    _longPressedDelay = value;
                    OnPropertyChanged();
                }
            }
        }
        TimeSpan _longPressedDelay = TimeSpan.FromSeconds(0.5);


        const double LONG_PRESSED_MIN_SECONDS = 0.25;

        public virtual void OnLongPressed()
        {
            var e = new ClickableEventArgs(this, ClickableEventType.LongPressed);
            LongPressed?.Invoke(this, e);
            if (!e.Handled)
            {
                ClickableEventCommand?.Execute(e);
            }
        }

        public event EventHandler? LongPressed;

        /// <summary>
        /// Internal signal, e.g. to suppress toggle on a release;
        /// </summary>
        /// <remarks>
        /// Any real time work should subscribe to the event, not the property change.
        /// </remarks>
        protected bool IsLongPressDetected
        {
            get => _isLongPressDetected;
            set
            {
                if (!Equals(_isLongPressDetected, value))
                {
                    _isLongPressDetected = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isLongPressDetected = false;


        public static readonly BindableProperty ClickableEventCommandProperty =
            BindableProperty.Create(
                propertyName: nameof(ClickableEventCommand),
                returnType: typeof(ICommand),
                declaringType: typeof(ButtonBase),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is ButtonBase @this)
                    {
                        // Do something with @this.ClickableEventCommand
                    }
                });

        public ICommand ClickableEventCommand
        {
            get => (ICommand)GetValue(ClickableEventCommandProperty);
            set => SetValue(ClickableEventCommandProperty, value);
        }
    }
}
