using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using OPC.Preview.Portable.Events;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static IVSoftware.Portable.GlyphProvider;

namespace OPC.Preview.Portable.Models
{
    public class GroupBoxItemModel 
        : INotifyPropertyChanged
        , IContainerBindingContext
    {
        public GroupBoxItemModel(
            Enum member,
            GroupBoxItemStyle style, 
            IList? groupItems = null)
        {
            Member = member;
            if (member.GetCustomAttribute<DescriptionAttribute>()?.Description is string description
                && !string.IsNullOrWhiteSpace(description))
            {
                Text = description;
            }
            else
            {
                Text = member.ToString();
            }
            ItemStyle = style; 
            _groupItems = groupItems;
            PointerPressedCommand = new CommandPCL(OnPressed);
            PointerReleasedCommand = new CommandPCL(OnReleased);
            ClickableEventCommand = new CommandPCL<ClickableEventArgs>(OnClickableEvent);

            //PressedCommand = new CommandPCL(OnPressed);
            //ReleasedCommand = new CommandPCL(OnReleased);
        }
        IList? _groupItems;
        public object? ContainerBindingContext
        {
            get => _containerBindingContext;
            set
            {
                if (!Equals(_containerBindingContext, value))
                {
                    _containerBindingContext = value;
                    OnPropertyChanged();
                }
            }
        }
        object? _containerBindingContext = default;

        public GroupBoxItemStyle ItemStyle
        {
            get => _itemStyle;
            protected set
            {
                if (!Equals(_itemStyle, value))
                {
                    _itemStyle = value;
                    OnStyleChanged();
                    OnPropertyChanged();
                }
            }
        }
        GroupBoxItemStyle _itemStyle = default;

        public bool IsLabelStyle => ItemStyle == GroupBoxItemStyle.Radio;
        public bool IsButtonStyle => ItemStyle != GroupBoxItemStyle.Radio;

        protected virtual void OnStyleChanged()
        {
            switch (ItemStyle)
            {
                case GroupBoxItemStyle.String:
                    break;
                case GroupBoxItemStyle.Glyph:
                    break;
                case GroupBoxItemStyle.GlyphString:
                    break;
                case GroupBoxItemStyle.CheckBox:
                    if (IsChecked)
                    {
                        Icon = IconBasics.Checked;
                    }
                    else
                    {
                        Icon = IconBasics.Unchecked;
                    }
                    break;
                case GroupBoxItemStyle.Radio:
                    if(IsChecked)
                    {
                        Icon = IconRadio.DotCircled;
                    }
                    else
                    {
                        Icon = IconRadio.CircleThin;
                    }
                    break;
                default:
                    break;
            }
        }

        public ICommand ClickableEventCommand { get; }
        private void OnClickableEvent(ClickableEventArgs e)
        {
            switch (e.EventType)
            {
                case ClickableEventType.Pressed:
                    OnPressed(e);
                    break;
                case ClickableEventType.Released:
                    OnReleased(e);
                    break;
            }
        }

        public string? Text { get; }
        public ICommand PointerPressedCommand { get; }
        private void OnPressed(object? o)
        {
            IsPressed = true;
        }
        public ICommand PointerReleasedCommand { get; }
        private async void OnReleased(object? o)
        {
            IsPressed = false;

            this.SetModalResult(
                Text!, 
                Member, 
                endModal: IsDoublePressed || ItemStyle != GroupBoxItemStyle.Radio);
        }
        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                if (!Equals(_isPressed, value))
                {
                    _isPressed = value;
                    OnIsPressedChanged();
                    OnPropertyChanged();
                }
            }
        }

        protected virtual void OnIsPressedChanged()
        {
            if (IsPressed)
            {
                switch (ItemStyle)
                {
                    case GroupBoxItemStyle.String:
                        break;
                    case GroupBoxItemStyle.Glyph:
                        break;
                    case GroupBoxItemStyle.GlyphString:
                        break;
                    case GroupBoxItemStyle.CheckBox:
                        break;
                    case GroupBoxItemStyle.Radio:
                        Icon = IconRadio.Circle;
                        break;
                    default:
                        this.ThrowFramework<NotSupportedException>($"The {ItemStyle.ToFullKey()} case is not supported.");
                        break;
                }
            }
            else
            {
                switch (ItemStyle)
                {
                    case GroupBoxItemStyle.String:
                        break;
                    case GroupBoxItemStyle.Glyph:
                        break;
                    case GroupBoxItemStyle.GlyphString:
                        break;
                    case GroupBoxItemStyle.CheckBox:
                        IsChecked = !IsChecked;
                        break;
                    case GroupBoxItemStyle.Radio:
                        var groupItems =
                            _groupItems
                            ?.OfType<GroupBoxItemModel>()
                            .ToArray()
                            ?? [];
                        if (groupItems.Length < 2)
                        {
                            IsChecked = !IsChecked;
                        }
                        else
                        {
                            foreach (var item in _groupItems?.OfType<GroupBoxItemModel>() ?? [])
                            {
                                if (ReferenceEquals(this, item))
                                {
                                    IsChecked = true;
                                    Icon = IconRadio.DotCircled;
                                }
                                else
                                {
                                    item.IsChecked = false;
                                }
                            }
                        }
                        break;
                    default:
                        this.ThrowFramework<NotSupportedException>($"The {ItemStyle.ToFullKey()} case is not supported.");
                        break;
                }
            }
        }

        bool _isPressed = false;

        public bool IsDoublePressed
        {
            get => _isDoublePressed;
            set
            {
                if (!Equals(_isDoublePressed, value))
                {
                    _isDoublePressed = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isDoublePressed = false;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                IsDoublePressed = value && _isChecked;
                if (!Equals(_isChecked, value))
                {
                    _isChecked = value;
                    OnIsCheckedChanged();
                    OnPropertyChanged();
                }
            }
        }

        protected virtual void OnIsCheckedChanged()
        {
            Icon = IsChecked ? IconRadio.DotCircled : IconRadio.CircleThin;
        }

        bool _isChecked = false;

        public Enum? Member
        {
            get => _member;
            set
            {
                if (!Equals(_member, value))
                {
                    _member = value;
                    OnPropertyChanged();
                }
            }
        }
        Enum? _member = default;


        public Enum? Icon
        {
            get => _icon;
            set
            {
                if (!Equals(_icon, value))
                {
                    _icon = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsIconVisible));
                }
            }
        }
        Enum? _icon = null;

        public bool IsIconVisible => Icon is not null;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
