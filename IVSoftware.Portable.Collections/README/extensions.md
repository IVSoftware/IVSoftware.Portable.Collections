

```
// <summary>
/// Fluent Extension
/// </summary>
public static T WithCollectionChangedEvent<T>(
    this T @this,
    NotifyCollectionChangedEventHandler onCollectionChanged
) 
where T : IObservablePreviewCollection
{
    @this.CollectionChanged += onCollectionChanged;
    return @this;
}        
```

```
/// <summary>
/// Fluent Extension
/// </summary>
public static T WithPreviewCollectionChangeEvents<T>(
    this T @this,
    NotifyCollectionChangingEventHandler onCollectionChanging,
    NotifyCollectionChangedEventHandler onCollectionChanged
) 
where T : IObservablePreviewCollection
{
    @this.CollectionChanging += onCollectionChanging;
    @this.CollectionChanged += onCollectionChanged;
    return @this;
}
```