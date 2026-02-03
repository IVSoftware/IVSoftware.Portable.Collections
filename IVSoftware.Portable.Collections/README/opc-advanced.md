# [<](../../README.md)

## `ObservablePreviewCollection` (OPC) - Advanced Features

OPC strives to be a faithful to the expected behavior of `ObservableCollection<T>` without springing surprises. Its advanced features are offered on an opt-in basis by setting flags on the `OptimizationMode` property.

___

### `UseCacheForContains`

Enables OPC tracking of add and remove events to maintain a hash set of distinct members, and when a duplicate is added it maintains a separate reference count. This information is then used to expose a `Contains` method that is O(1).

___

### `TrackItemPropertyChanges`

Enables OPC tracking of collection mutations in order to detect items that implement `INotifyPropertyChanging` and `INotifyPropertyChanged` and subscribe to those events.

___

## Subset Tracking

When the collection is tracking item property changes, those events can be leveraged to model logical group membership without altering the structure or visibility of the underlying collection.

A `TrackContext` maintains a live, property-driven subset whose membership is derived from item state rather than index position. The owning collection remains structurally intact, while the track context exposes a stable snapshot of items that currently satisfy a predicate. Membership is updated incrementally in response to both collection mutations and item property changes.

Track contexts can be created explicitly, but in most cases they are instantiated implicitly by applying a `[Track]` attribute to a compatible item property. When activated, the collection automatically enables the required optimization modes and routes item property changes through the tracking pipeline.

 For example, this snippet tracks `SelectedItems` in the portable model and allows the UI view to _sink_ it. This is an alternative to the common (but platform-specific) approach where the collection view itself acts as the _source_ of selection state.

The example also includes a tracked `IsChecked` property, but the two illustrate different tracking domains. `IsChecked` is a binary state that tracks directly as a `bool`, whereas `Selection` represents a stateful, non-zero domain with values such as `Exclusive`, `Multi`, `Primary`, and `Pressed`. These richer states can then be used to inform visual styling without coupling that logic to the view layer.


```
public class ItemCardModel : SelectableQFModel, INotifyPropertyChanging
{
    [Track(TrackMode.Single, WherePredicate.IsNotZero)]
    public new ItemSelection Selection
    {
        get => base.Selection;
        set
        {
            var e = new PropertyChangingPreviewEventArgs<ItemSelection>(
                oldValue: base.Selection,
                newValue: value);
            PropertyChanging?.Invoke(this, e);
            if (!e.Cancel)
            {
                base.Selection = e.NewValue;
            }
        }
    }

    [Track(TrackMode.Multiple, WherePredicate.IsTrue)]
    public new bool IsChecked
    {
        get => base.IsChecked;
        set
        {
            var e = new PropertyChangingPreviewEventArgs<bool>(
                oldValue: base.IsChecked,
                newValue: value);
            PropertyChanging?.Invoke(this, e);
            if (!e.Cancel)
            {
                base.IsChecked = e.NewValue;
            }
        }
    }
    ...
}
```

___

## List Filtering

In contrast to property tracking, which plays an advisory role, list filtering actively presents a subset view of the current recordset while keeping the underlying data intact. When filters are removed, the UI returns to the original, unfiltered state.

Because filtering modifies the visible surface of the collection, structural operations performed while filtered are subject to heuristic interpretation.
___
_For example, consider a case where only the first four items (out of ten) are visible. Adding a new item assigns it an index of 4, which is absolute in the sense that the restored collection will display it in the 5th position._
___

Filtering in OPC is implemented as an aggregate of SQL `WHERE` clauses evaluated against an in-memory SQLite database. This allows predicates to be composed flexibly and evaluated efficiently without mutating the underlying collection.

Because the underlying SQLite engine requires a primary key, filtering is only enabled when the generic type parameter `<T>` exposes a property decorated with the `[PrimaryKey]` attribute. 


```
// using SQLite

[PrimaryKey]
public override string Id { get; set; } = Guid.NewGuid().ToString();
```

If no primary key property can be resolved for `T`, filtering is disabled entirely.

Filters are activated programmatically by supplying one or more predicate enums. The enum type itself is unconstrained, but each participating member must declare a `[Where]` attribute defining the SQL predicate it represents:


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

Then, members are activated and deactivated using the respective method, e.g.:

```
void ActivateFilters(Enum stdPredicate, params Enum[] more);
```

Multiple predicates are combined to form a single query expression, allowing filters to be layered, toggled, and recomposed at runtime.
___
_When the filter is enabled, changes to the **visible** list are still tracked but this process is somewhat heuristic by nature. That is, removing an item is deterministic, but adding a new item attempts to interpolate the intended index in a list that is not fully visible. To disallow changes while filtered, simply subscribe to the `CollectionChanging` event and set `e.Cancel` to true._
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

