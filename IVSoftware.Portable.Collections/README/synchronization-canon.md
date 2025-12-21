## Synchronization

The document affects:

- Distinctifier
- ModalContexts (e.g. for SelectedItems and IsChecked tracking)
- Filtering
- MarkdownContext

---

## Common Requirements:

Type `<T>` where

1. Has attribute named `[PrimaryKey]`.
2. Implements `INotifyPropertyChanged` and `INotifyCollectionChanged`
3. 

## IFollow

1. `ResetSync()` - Rebuilds itself from owner.

## Filter Tracking

1. Happens when MarkdownContext has filter flag.

## Suspensions

1. The `DHostSuspendSynchronization` tracks most _other_ dhost suspensions, too.
2. Runs `ResetSync()` on all such objects.



