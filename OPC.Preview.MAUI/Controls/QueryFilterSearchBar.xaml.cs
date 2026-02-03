using CommunityToolkit.Maui.Core.Platform;
using IVSoftware.Portable.SQLiteMarkdown;
using System.ComponentModel;
using System.Windows.Input;

namespace OPC.Preview.Maui.Controls;

public partial class QueryFilterSearchBar : ContentView
{
	public QueryFilterSearchBar()
	{
		InitializeComponent();
        this.QueryFilterEntry.Completed += async (sender, e) =>
        {
#if !MACCATALYST15_0_OR_GREATER
            QueryFilterEntry.Unfocus();
            for (int i = 0; i < 10; i++)
            {
                if(KeyboardExtensions.IsSoftKeyboardShowing(QueryFilterEntry))
                {
                    await Task.Delay(100);
                }
                else
                {
                    break;
                }
            }
#endif
            switch (MarkdownContext.SearchEntryState)
            {
                case SearchEntryState.QueryEN:
                    Commit?.Execute(MarkdownContext);
                    break;
            }
        };
        IsEnabled = false;
        Loaded += (sender, e) =>
        {
            Dispatcher.Dispatch(() => IsEnabled = true);
        };
    }

    public static readonly BindableProperty MarkdownContextProperty =
            BindableProperty.Create(
                propertyName: nameof(MarkdownContext),
                returnType: typeof(MarkdownContext),
                declaringType: typeof(QueryFilterSearchBar),
                defaultValue: default,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    if (bindable is QueryFilterSearchBar @this)
                    {
                        @this.MarkdownContext.PropertyChanged += localOnMarkdownContextPropertyChanged;
                    }
                    void localOnMarkdownContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
                    {
                        switch (e.PropertyName)
                        {
                            case nameof(MarkdownContext.InputText):
                                break;
                        }
                    }
                });

    public MarkdownContext MarkdownContext
    {
        get => (MarkdownContext)GetValue(MarkdownContextProperty);
        set => SetValue(MarkdownContextProperty, value);
    }
    public static readonly BindableProperty CommitProperty =
    BindableProperty.Create(
        propertyName: nameof(Commit),
        returnType: typeof(ICommand),
        declaringType: typeof(QueryFilterSearchBar),
        defaultValue: default(ICommand),
        defaultBindingMode: BindingMode.OneWay);

    public ICommand? Commit
    {
        get => (ICommand?)GetValue(CommitProperty);
        set => SetValue(CommitProperty, value);
    }

    public SearchEntryState SearchEntryState => MarkdownContext.SearchEntryState;

    public FilteringState FilteringState => MarkdownContext.FilteringState;
}