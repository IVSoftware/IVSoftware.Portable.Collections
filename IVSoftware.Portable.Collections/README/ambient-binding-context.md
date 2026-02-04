# [<](../../README.md)

## `IAmbientBindingContext`

`IAmbientBindingContext` is a lightweight contract for propagating a shared *ambient* binding context through a graph of related objects without hard-coding parent references or relying on framework-specific binding infrastructure.

Think of it as a polite, opt-in way for objects to say: "If there is a binding context upstream of me, I would like to know about it."

This pattern is especially useful in collection-driven UI models (for example, MAUI, WPF, or WinForms hybrids) where items are created, added, and removed dynamically, yet still need access to a higher-level binding context.

---

### The core idea

An object implementing `IAmbientBindingContext` exposes a single property:

```
object? AmbientBindingContext { get; set; }
```

That property may point to:
- a plain binding context object (for example, a view model), or
- another `IAmbientBindingContext`, forming a *chain*.

By following that chain, consumers can walk "upward" through successive ambient providers until no further context is available.

---

### Collection-level propagation

`ObservablePreviewCollection<T>` acts as an ambient binding *provider* for its items.

When its `AmbientBindingContext` changes:

- All existing items that implement `IOPAmbientBindingContext` are immediately updated.
- Newly added items automatically receive the current ambient context during `NotifyCollectionChangedAction.Add`.

This ensures that the collection behaves like a living conduit rather than a static initializer.

Key properties of this behavior:

- No assumptions about item lifetimes.
- No requirement that items know *who* owns them.
- No coupling to XAML or framework binding lifecycles.

The collection owns the responsibility of keeping the ambient context coherent.

---

### Traversal and discovery

To make the chain usable, `CollectionExtensions` provides two traversal helpers.

`Ancestors(...)` walks the ambient chain by repeatedly following
`AmbientBindingContext` **only when it is itself an `IOPAmbientBindingContext`**.
Traversal stops cleanly when the chain ends.

This produces a deterministic, linear ancestry with no reflection and no global state.

`AncestorAmbientBindingContextsOfType<T>(...)` builds on that idea by projecting
each ancestor’s `AmbientBindingContext` and filtering by type.

The result is a strongly-typed, lazy sequence of matching contexts, ordered from
nearest to farthest.

---

### Design intent

This pattern is deliberately:

- **Non-hierarchical**  
  There is no requirement that objects be UI parents, logical parents, or collection owners.

- **Framework-agnostic**  
  It works the same way in tests, previews, headless models, or UI layers.

- **Composable**  
  Any object may participate or opt out simply by implementing the interface.

- **Predictable**  
  Traversal is explicit, linear, and side-effect free.

In short, `IAmbientBindingContext` enables contextual awareness without introducing
ownership, static state, or fragile assumptions about object graphs.

---

### When to use it

Use this pattern when:

- Items need access to shared context but should not *own* it.
- Parent references would introduce cycles or lifetime hazards.
- Binding frameworks are present but insufficiently expressive.
- You want testable, explicit context flow rather than implicit magic.

It is not a replacement for normal data binding; it is a structural tool for
cases where binding alone cannot describe the relationships cleanly.

---

### Mental model

If normal binding is "data flows downward," ambient binding contexts are
"signals drifting upward.
