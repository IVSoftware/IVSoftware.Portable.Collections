using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using Microsoft.Maui.Controls;
using OPC.Preview.Maui.Models;
using OPC.Preview.Portable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static IVSoftware.Portable.GlyphProvider;

namespace OPC.Preview.Maui.Controls;

public partial class InfoOverlay
    : ContentView
    , IInfoOverlay
{
    public InfoOverlay()
    {
        InitializeComponent();
        IsVisible = false;
        // Unfocus child items bu focusing container
        Loaded += (sender, e) => Focus();
    }
    public enum ReservedMessageId { Default }
    public enum DSAOption
    {
        ShowMessageOnce,
        ShowMessageAlways,
        ShowDSAOptions,
    }
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        switch (propertyName)
        {
            case nameof(IsVisible):
                OnIsVisibleChanged();
                break;
        }
    }

    protected virtual void OnIsVisibleChanged()
    {
        if (IsVisible)
        {
            _awaiter.Wait(0);
        }
        else
        {
            _awaiter.Wait(0);
            _awaiter.Release();
        }
    }

    public TaskAwaiter GetAwaiter()
    {
        return Task.Run(async () =>
        {
            await _awaiter.WaitAsync();
            _awaiter.Release();
        }).GetAwaiter();
    }

    private void OnOverlayTapped(object sender, TappedEventArgs e) => InfoText = string.Empty;

    private void OnToggleDSA(object sender, TappedEventArgs e)
    {
        throw new NotImplementedException("ToDo");
    }

    public void ForceVisit(Enum @enum) => _visited.Add(@enum);

    private readonly HashSet<Enum> _visited = new();

    /// <summary>
    /// Sets Text plus Visibility.
    /// </summary>
    /// <remarks>
    /// Internal, in order to force a DSA check before showing.
    /// </remarks>
    internal string InfoText
    {
        get => _infoText;
        set
        {
            if (!Equals(_infoText, value))
            {
                _infoText = value;
                IsVisible = !string.IsNullOrEmpty(_infoText);
            }
        }
    }
    string _infoText = string.Empty;
    SemaphoreSlim _awaiter = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Sets Text plus Visibility after checking DSA.
    /// </summary>
    /// <remarks>
    /// Internal, in order to force a DSA check before showing.
    /// </remarks>
    [Canonical]
    public async Task<bool> ShowInfo(
        string? info, 
        Enum messageId,
        DSAOption dsaOption = DSAOption.ShowMessageOnce)
    {
        if(Equals(ReservedMessageId.Default, messageId))
        {
            // No DSA on default id.
            if (dsaOption != DSAOption.ShowMessageAlways)
            {
                this.Advisory(
                    $"DSA option is not available when Id={ReservedMessageId.Default.ToFullKey()}");
                dsaOption = DSAOption.ShowMessageAlways;
            }
        }
        else
        {
            if (messageId.GetCustomAttribute<InfoTextAttribute>() is { } attr)
            {
                switch (dsaOption)
                {
                    case DSAOption.ShowMessageOnce:
                        if (!_visited.Add(messageId))
                        {
                            return false;
                        }
                        break;
                    case DSAOption.ShowMessageAlways:
                    case DSAOption.ShowDSAOptions:
                        break;
                    default:
                        this.ThrowHard<NotSupportedException>($"The {dsaOption.ToFullKey()} case is not supported.");
                        break;
                }
                Rows.Clear();
                var message = attr.Message.Replace("%info%", info);
                foreach (var lineSpan in message.TrimStart().EnumerateLines())
                {
                    var trimmed = lineSpan.TrimStart();

                    if (trimmed.IsEmpty)
                    {
                        Rows.Add(new InfoOverlayRow
                        {
                            Style = InfoOverlayRowStyle.Separator
                        });
                        continue;
                    }

                    if (trimmed.StartsWith("# "))
                    {
                        Rows.Add(new InfoOverlayRow
                        {
                            Style = InfoOverlayRowStyle.Header,
                            Text = trimmed[2..].ToString()
                        });
                        continue;
                    }

                    if (trimmed.StartsWith("- "))
                    {
                        Rows.Add(new InfoOverlayRow
                        {
                            Style = InfoOverlayRowStyle.BulletText,
                            Text = trimmed[2..].ToString()
                        });
                        continue;
                    }

                    Rows.Add(new InfoOverlayRow
                    {
                        Style = InfoOverlayRowStyle.Text,
                        Text = trimmed.ToString()
                    });
                }
            }
            else
            {
                this.ThrowHard<InvalidOperationException>($"Missing [InfoText] on member: {messageId.ToFullKey()}.");
            }
        }

        var builder = new List<string>();

        if(messageId.GetCustomAttribute<InfoTextAttribute>()?.Message is { } preview 
            && !string.IsNullOrWhiteSpace(preview))
        {
            builder.Add(preview);
        }

        if (!builder.Any())
        {
            this.ThrowHard<ArgumentException>(
                $"Requires non-empty value for '{nameof(info)}' supplied by arg or by attr.");
            return false;
        }
        var concat = string.Join(Environment.NewLine, builder);
        var e = new BeforeShowInfoEventArgs(messageId, concat);
        BeforeShowInfo?.Invoke(this, e);
        if (!e.Cancel)
        {
            IsVisible = true;
            InfoText = concat;
            // [Careful]
            // If the mode is 'ShowMessageOnce' there is no need to say don't show again. It won't.
            // If the mode is 'ShowMessageAlways' there is no need to show an option. Leaving:
            DSAOptions.IsVisible = dsaOption == DSAOption.ShowDSAOptions;
        }
        await this;
        return true;
    }
    public async Task<bool> ShowInfo(
        Enum messageId,
        DSAOption dsaOption = DSAOption.ShowMessageOnce)
        => await ShowInfo(null, messageId, dsaOption);
    public async Task<bool> ShowInfo(
        string info)
        => await ShowInfo(info, ReservedMessageId.Default);

    public event EventHandler<BeforeShowInfoEventArgs>? BeforeShowInfo;
    public ObservableCollection<InfoOverlayRow> Rows { get; } = new();

    public static readonly BindableProperty CurrentInfoProperty =
            BindableProperty.Create(
                propertyName: nameof(CurrentInfo),
                returnType: typeof(Enum),
                declaringType: typeof(InfoOverlay),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: async (bindable, oldValue, newValue) =>
                {
                    if (bindable is InfoOverlay @this)
                    {
                        if(newValue is Enum @enum)
                        {
                            await @this.ShowInfo(@enum);
                        }
                        else
                        {
                            @this.IsVisible = false;
                        }
                    }
                });

    public Enum CurrentInfo
    {
        get => (Enum)GetValue(CurrentInfoProperty);
        set => SetValue(CurrentInfoProperty, value);
    }
    public void SetDSA(
        Enum messageId,
        DSAOption dsaOption = DSAOption.ShowMessageOnce)
    {
        _visited.Add(messageId);
    }
}
public class BeforeShowInfoEventArgs : CancelEventArgs
{
    public BeforeShowInfoEventArgs(Enum messageId, string info, bool showDSAOptions = false)
    {
        MessageId = messageId;
        Info = info;
    }
    public Enum MessageId { get; }

    public string Info { get; set; }
    public bool ShowDSAOptions { get; set; }
}