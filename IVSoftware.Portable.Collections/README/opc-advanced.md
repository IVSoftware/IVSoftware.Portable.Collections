# [<](../../README.md)

## `ObservablePreviewCollection` (OPC) - Advanced Features

OPC strives to be a faithful to the expected behavior of `ObservableCollection<T>` without springing surprises. Its advanced features are offered on an opt-in basis by setting flags on the `OptimizationMode` property.

___

### `UseCacheForContains`

Enables OPC tracking of add and remove events to maintain a hash set of distinct members, and when a duplicate is added it maintains a separate reference count. This information is then used to expose a `Contains` method that is O(1).

___

### `TrackItemPropertyChanges`

Enables OPC tracking of add and remove events, detecting items that implement `INotifyPropertyChanging` and `INotifyPropertyChanged` and subscribing to those events.

___

## Subset Tracking

___

## List Filtering

```
public enum StdPredicate
{
    /// <summary>
    /// Items that are affirmatively checked. As in "show only the items that are checked".
    /// </summary>
    [Where("IsChecked = 1")]
    IsChecked,

    /// <summary>
    /// Items that are affirmatively unchecked. As in "show only the items that are unchecked".
    /// </summary>
    [Where("IsChecked = 0")]
    IsUnchecked,
}
```
___
_When the filter is enabled, changes to the **visible** list are still tracked but this process is somewhat heuristic by nature. That is, removing an item is deterministic, but adding a new item attempts to interpolate the intended index is a list that is not fully visible. It's easy to disable changed when filtered, however. Simply subscribe to the `CollectionChanging` event of the list and set `e.Cancel` to true to prevent any changes from occurring._
___

### Performance Cost of Filters

Filters require a copy which for large collections might not be tenable.

```
protected virtual void ApplyChanges(NotifyCollectionChangingEventArgs e)
{
    // May affect performance. Requires opt-in.
    if (OptimizationMode.HasFlag(ListOptimizationMode.EnableFilterContexts))
    {
        PreChangeSnapshot = this.ToArray();
    }
    ...
}
```

___ 

## Filtering using SQLite Markdown

This NuGet has [IVSoftware.Portable.SQLiteMarkdown](https://github.com/IVSoftware/IVSoftware.Portable.SQLiteMarkdown.git) as a dependency and can use a shorthand in a textbox for filtering the list dynamically.

At this point, it becomes necessary to track Query-Filter states, and the `MarkdownContext` property is the provider of this state machine.

---










