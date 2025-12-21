using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Collections
{
    public class EventContract
    {
        public Type? EventType
        {
            get => _eventType;
            set
            {
                if (!Equals(_eventType, value))
                {
                    _eventType = value;
                    OnPropertyChanged();
                }
            }
        }
        Type? _eventType = default;
        public Type? NewItems
        {
            get => _newItems;
            set
            {
                if (!Equals(_newItems, value))
                {
                    _newItems = value;
                    OnPropertyChanged();
                }
            }
        }
        Type? _newItems = null;

        public Type? OldItems
        {
            get => _oldItems;
            set
            {
                if (!Equals(_oldItems, value))
                {
                    _oldItems = value;
                    OnPropertyChanged();
                }
            }
        }
        Type? _oldItems = null;

        public override string ToString()
        {
            var builder = new List<string>();

            void add(string name, Type? type)
            {
                if (type is not null)
                {
                    builder.Add($"{name}: {type.Name}");
                }
            }

            add(nameof(EventType), EventType);
            add(nameof(NewItems), NewItems);
            add(nameof(OldItems), OldItems);

            if (builder.Count == 0)
            {
                return "{}";
            }

            return $"{{{Environment.NewLine}  {string.Join($",{Environment.NewLine}  ", builder)}{Environment.NewLine}}}";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Index.Clear();

            foreach (var unk in new Type?[]
            {
                EventType,
                NewItems,
                OldItems,
            })
            {
                Index[unk.GetType().Name] = unk;
            }
            if (ReferenceEquals(sender, this))
            {
                PropertyChanged?.Invoke(sender, e);
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public Dictionary<string, Type?> Index { get; } = new();
    }
}
