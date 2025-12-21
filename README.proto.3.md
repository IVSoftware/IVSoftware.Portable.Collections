## IVSoftware.Portable.Collections [GitHub](https://github.com/IVSoftware/IVSoftware.Portable.Collections.git)

This package offers convenient solutions for everyday .NET problem domains.

### Highlights

- Notifying collection changes *before* they occur - while there is still time to act.
- Use pattern matching in place of `TryGetValue` for tolerant dictionary access.
- On-demand activation of missing keys as a guarantee.
- Consolidating access to caches when multiple sources exist.
- Reduce UI churn through intelligent, preview-aware ranging.

It also comes with side benefits - behaviors developed in support of these collections that stand on their own beyond the immediate domain.

- An interceptable `Throw` pattern giving NuGet package clients improved control of outcomes.
- The `NotifyCollectionChangingEventArgs` type itself, reusable in any collection that benefits from pre-change notification.

___

## Table of Contents

| # | Included in this Package |  |
|---|------------------|--------|
| 1 | [NotifyCollectionChangingEventArgs](#notifycollectionchangingeventargs) | [ReadMe](./IVSoftware.Portable.Collections/README/notify-collection-changing-event-args.md) |
| 2 | [NotifyPreviewCollectionChangingEventArgs](#notifypreviewcollectionchangingeventargs) | [ReadMe](./IVSoftware.Portable.Collections/README/notify-preview-collection-changing-event-args.md) |
| 3 | [ObservablePreviewCollection](#observablepreviewcollection) | [ReadMe](./IVSoftware.Portable.Collections/README/observable-preview-collection.md) |
| 4 | [ObservableDictionary](#observabledictionary) | [ReadMe](./IVSoftware.Portable.Collections/README/observable-dictionary.md) |
| 5 | [TolerantDictionary](#observabletolerantdictionary) | [ReadMe](./IVSoftware.Portable.Collections/README/observable-tolerant-dictionary.md) |
| 6 | [InsistentDictionary](#observableinsistentdictionary) | [ReadMe](./IVSoftware.Portable.Collections/README/observable-insistent-dictionary.md) |
| 7 | [BriskDictionary](#observablebriskdictionary) | [ReadMe](./IVSoftware.Portable.Collections/README/observable-brisk-dictionary.md) |
| 8 | [Exception Handling (Hard, Soft, Advisory)](#exception-handling-hard-soft-and-advisory) | [ReadMe](./IVSoftware.Portable.Collections/README/exception-handling.md) |
| 9 | [Disposable Batch Blocks](#disposable-batch-blocks) | [ReadMe](./IVSoftware.Portable.Collections/README/disposable-batch-blocks.md) |

---

### NotifyCollectionChangingEventArgs

An immutable, pre-change counterpart to `NotifyCollectionChangedEventArgs`, this abstract base class represents a collection mutation *before* it commits. It exposes the familiar `Action`, `NewItems`, `OldItems`, and index properties that describe the intended modification while allowing subscribers to observe or cancel it. Implements `INotifyPropertyChanged` for fine-grained tracking of internal state. Instances are produced via `NotifyPreviewCollectionChangingEventArgs`, ensuring immutability and consistent pre-change semantics.

While `NotifyCollectionChangedEventArgs` is ubiquitous in observable collections, `NotifyCollectionChangingEventArgs` remains rare, not for lack of utility, but because once handlers can mutate event arguments, the semantic line between "what the collection intends" and "what handlers propose" blurs. Nothing illustrates this better than `null`, which must now mean either "unset" or "explicitly null." That distinction matters: should the handler's `null` be ignored, or should it add a null element?

The class's central value is its capacity to cancel an impending change based on the proposed mutation. It preserves the familiar shape and behavior of `NotifyCollectionChangedEventArgs` while restoring a clear pre-commit contract.

---

### NotifyPreviewCollectionChangingEventArgs

Here,the limitations of the .NET-Like `NotifyCollectionChangingEventArgs` class are addressed through the concept of **Coercion**. Simply stated, as an alternative to exposing the underlying value to arbitrary and potentially damaging changes, the `Preview` variant provides a `TryCoerceValue` method that manages type safety. Safe changes are greenlighted and the wrapper that contains it is flagged as modified.

As an abstract base class, the `Non-Preview` version of the event was always covariant with the `Preview` version. To elevate it to fully writeable simply upcast the event, for example using the syntactic sugar expressed in this canonical pattern:

```
var list = new ObservablePreviewCollection<string>();

list.CollectionChanging += (sender, e) =>
{
    var ePre = e.EnableCoerce();
    if(ePre.GetNewItem() is { } coercible)
    {
        coercible.Coerce("NewValue");
    }
};

list.Add("OldValue");

Debug.Assert(list.Single() == "NewValue", "Expecting coerced value.");
```

The `Debug` statement is showing that the coerced new value is the one that enters the collection. Alternatively, the `Cancel` property could be set leaving the empty list untouched.

___

## Conceptual Model: How Preview Events Redefine Collection Semantics

Before diving deeper, it helps to understand why this system does not collapse under the usual "kitchen sink" problem. Rather than bolting features onto `Add` or `Remove`, the preview layer reframes mutation as a single, disciplined stage where intent is proposed, examined, and, when necessary, rewritten.

For most developers, the experience remains a direct analog to .NET’s `NotifyCollectionChangedEventArgs`, with the added ability to cancel an operation. But once a handler explicitly opts into the coercible preview model, it assumes a superuser role: full authority to reinterpret the proposed change, adjust its details, or replace it outright. This elevation is deliberate, never accidental, and the framework executes the resulting proposal exactly as described.


### 1. The Framework Proposes
Every mutation begins with an intent. For example:

- Add item X at index I  
- Remove item at index I  
- Replace old with new  
- Move items  
- Reset or batch changes  

The framework creates a preview event describing that intent in the same structured way that `NotifyCollectionChangedEventArgs` would describe the completed change.

### 2. The Handler Decides
Handlers see the proposed change and choose how far they want to participate.

**Basic usage**  
Developers who simply want veto power can set:

```
e.Cancel = true;
```

The change never occurs.

**Advanced usage**  
Developers who want to inspect and alter the proposal explicitly opt in:

```
var ePre = e.EnableCoerce();
if (ePre.GetNewItem() is { } coercible)
{
    coercible.Coerce("NewValue");
}
```

Opting in is intentional. Nothing is ever coerced by accident.

Handlers may:

- change proposed values  
- change proposed keys  
- adjust indices  
- reinterpret the action type  
- introduce batch semantics  
- or replace the operation entirely  

All changes are tracked through `ModifiedFlag` so the collection can respond predictably.

### 3. The Proposal Becomes the Authority
One of the subtle but important design choices is that the original method call (for example `Add(item)`) does not force the framework to recreate the original semantics after the preview handler has modified the event.

Instead:

- The preview event becomes the single authoritative description of the final mutation.
- The collection applies the handler’s proposal directly.
- No twisting, reconciling, or mapping back to the original method call is performed.

This avoids the pitfalls common in pre-change systems, where the framework tries to preserve the caller’s intent but also tries to honor arbitrary handler mutations. That negotiation model collapses under complexity. This model does not.

### 4. Coercion Without Chaos
To avoid unsafe mutation, values are wrapped in coercible types (`ICoercibleValuePreview`, `ICoercibleIndexPreview`, etc.). These wrappers support:

- type-checked coercion  
- contract detection (value, dictionary entry, etc.)  
- automatic unwrapping into the BCL-shaped lists required by the base class  
- modifications flagged for downstream inspection  

Handlers gain the power to rewrite the proposal without risking semantic corruption.

### 5. After the Preview, the Change Applies Cleanly
Once the preview event returns:

- If cancelled, nothing happens.  
- If modified, the collection adopts the new proposal exactly.  
- If untouched, the original intent applies.  

The final mutation is simpler than the preview phase. It is a direct execution of whatever the handler described.

This centralizes all complexity in the preview stage, leaving the execution stage clean, predictable, and fully aligned with the BCL event model.

### Summary
The preview system transforms collection mutation into a three-phase pipeline:

1. Framework proposes.  
2. Handler inspects, cancels, or rewrites the proposal.  
3. Framework executes the proposal exactly as described.

This is not a negotiation. It is a clear, disciplined rewrite stage resembling a compiler pass. The result is a powerful, safe, and predictable system for observing and transforming collection changes before they take effect.




## Extensions

This package provides fluent extension methods for easier event subscription.

[Extensions](./IVSoftware.Portable.Collections/README/extensions.md)

___


Below this line, the document is under development and has the following provisional links.

[Brisk Signature Equality](./IVSoftware.Portable.Collections/README/brisk-signature-equality.md)


[Insistent Dictionary Pattern](./IVSoftware.Portable.Collections/README/insistent-dictionary-pattern.md)

[Tolerant Dictionary Pattern](./IVSoftware.Portable.Collections/README/tolerant-dictionary-pattern.md)

[Open Issues](./IVSoftware.Portable.Collections/README/open-issues.md)


The subreads on GitHub will be found at this root:

[(SAVE) SUBREADS](https://github.com/IVSoftware/IVSoftware.Portable.Collections/blob/master/IVSoftware.Portable.Collections/README)


