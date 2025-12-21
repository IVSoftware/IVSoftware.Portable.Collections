# IVSoftware.Portable.Collections

### Event-Aware Collections for Developers Who Need to Intervene

Collections are one of .NET’s oldest abstractions, but their event model has always been one step behind.  
Developers can react to changes — but rarely influence them. This package closes that gap by introducing true *pre-change awareness*: the ability to inspect, modify, or even cancel a pending mutation before it becomes reality.  

The foundation is a new canonical interface, `INotifyCollectionChanging`, which complements `INotifyCollectionChanged` by representing the *moment before* the change.  
Its companion interface, `ICoercible`, defines how the pending value can be negotiated through explicit, type-safe contracts.

Together, these interfaces turn ordinary collections into cooperative participants in your application’s state management.

## IVSoftware.Portable.Collections

### Observable Collections and Dictionaries with True Pre-Change Awareness

This NuGet package introduces observable collections and dictionaries built on a new canonical interface, `INotifyCollectionChanging`. Unlike the standard `INotifyCollectionChanged`, which reports what *has already happened*, this interface describes what is *about to happen* -- providing a genuine pre-event stage where developers can still influence or cancel a pending mutation.  

Existing "preview" patterns, such as some interpretations of `INotifyPropertyChanging`, often fall short because they announce change without permitting intervention. `IVSoftware.Portable.Collections` resolves that by pairing its event model with a companion interface, `ICoercible`. This companion defines clear, enum-based contracts that expose what can be adjusted *before the fact*, giving consumers precise, contextual control as the collection prepares to change.  

### The Promise of ICoercible

`ICoercible` is more than a way to tweak values before a collection commits its change. It is the seed of a larger idea -- that collections can participate in their own population.

When coercion is present, the framework no longer treats a missing entry as failure. Instead, it can ask: *what would satisfy this request?* From that simple question emerge powerful behaviors -- on-demand resolutions, dynamic activations, and self-healing lookups that generate instances only when they are needed.

In that light, coercion is not just about interception; it is about collaboration. The collection and its subscribers share responsibility for determining what "should exist" in a given moment. This is how a pre-change event evolves from mere notification into the foundation for on-demand instantiation and intelligent caching.

___
### Understanding Coercion with `NotifyCollectionChangingAction.Replace` Example

In a normal `Replace` event, the old and new values are fixed — the collection is merely reporting what has already changed.  
With coercion, the "new value" can itself be `ICoercible`, meaning it can negotiate its contents before the update is applied.

A typical handler will query the event to determine whether a single item or an IList is expected:

```csharp
void OnCollectionChanging(object? sender, NotifyCollectionChangingCancelEventArgs e)
{
    if (e.Action == NotifyCollectionChangingAction.Replace &&
        e.GetCoercible<CoercibleValue>() is { } coercible)
    {
        coercible.Coerce(CoercibleValueContract.Value, "AdjustedValue");
    }
}
```

Here the `CoercibleValue` represents the pending replacement. The handler requests a controlled adjustment through the contract rather than rebuilding the event or subclassing the collection.  
Some actions deliver a coercible `IList`, letting the same pattern apply to ranges.  

This preserves the familiar event model while giving developers an active role in shaping the mutation as it happens.

## What This Package Offers

The five main collections include a prototype for an `ObservableCollection` with preview capabilities and batch mode, a fully observable `Dictionary` where missing keys can be created on demand, and three *opinionated* dictionary types that emphasize contract-based lookup semantics. Originally developed to support **Type Exchange Abstraction (TEA)** interoperability (slated for release in early 2026 as a separate NuGet package), their usefulness extends well beyond TEA — providing predictable, expressive behaviors for everyday collection design.

Readers are also encouraged to browse the [GitHub Project Repo](https://github.com/IVSoftware/IVSoftware.Portable.Collections.git) since everything is interface-based and subclassable for extensibility. In particular, the `MSTest` project not only shows how these models were validated, it also offers a wealth of usage examples.

| Section | Description |
|----------|-------------|
| [CoercibleObservableCollection\<T\>](#coercibleobservablecollectiont-example) | A familiar `ObservableCollection` variant that demonstrates true pre-change awareness. |
| [Dictionary Personality Summary](#dictionary-personality-summary) | Quick reference for tolerant, insistent, and brisk behaviors. |
| [Dictionary Personalities (Expanded Overview)](#dictionary-personalities-expanded-overview) | Detailed discussion of how each dictionary variant behaves and when to use it. |

___
## CoercibleObservableCollection<T> Example

*Coming soon.*  
This section will demonstrate how `CoercibleObservableCollection<T>` extends the standard `ObservableCollection<T>` with full `INotifyCollectionChanging` and `ICoercible` support.  
The example will illustrate:
- How coercible items expose their contracts during `CollectionChanging`.
- How handlers can modify or cancel pending adds, removes, and replaces.

___
## Dictionary Personality Summary

A first **Collections** release introducing three *opinionated* dictionary types that emphasize contract-based lookup semantics.  
Originally developed to support **Type Exchange Abstraction (TEA)** interoperability (slated for release in early 2026 as a separate NuGet package), their usefulness extends well beyond TEA — providing predictable, expressive behaviors for everyday collection design.

| Personality | Behavior |
|--------------|-----------|
| **Tolerant** | Returns `null` for missing keys without complaint. |
| **Insistent** | "Insists on" returning a non-null value. |
| **Brisk** | A non-generic insistent dictionary that guarantees an `IDictionary` for any key, and supports the `ComplexKey` pattern making it ideal for fast caching. |

___
## Dictionary Personalities (Expanded Overview)

### Unopinionated
A neutral baseline implementation adhering strictly to the `IDictionary` contract.
- **Implements:** `IDictionary`, `IDictionary<TKey, TValue>` where `TKey : notnull`
- **Behavior:** Standard .NET semantics. Missing keys raise `KeyNotFoundException`.  
- **Use Case:** When strictness is desired and no implicit recovery behavior is appropriate.

### Tolerant Variants

Throws no exceptions for missing keys, relying instead on pattern matching at the call site.

___

#### TolerantReturnNull
Tolerates missing keys by returning `default` instead of throwing exceptions.  
- **Implements:**  
  - `IDictionary`  
  - `IDictionary<TKey, TValue>` where `TKey : notnull`  
  - `ITolerant` / `ITolerantDictionary`  
- **Behavior:** Returns `null` for missing keys; no implicit insertion occurs.  
- **Why it matters:** Enables idiomatic pattern matching without verbose `TryGetValue` checks.
- **Example:**
  ```csharp
  if (tolerant[SomeKey] is { } exists)
  {
      // Safe pattern-matching access
  }
  ```
  
___
#### TolerantCreateDefaultEntry
A tolerant variant that auto-creates default entries when keys are missing.  
- **Behavior:** Inserts a new key with a `null` value rather than failing the lookup.  
- **Why it matters:** Useful for caching "missed attempts" or initializing sparse data structures.

___

### Insistent Variants

#### InsistentCreateDefaultEntry
An insistent contract that guarantees a non-null `TValue` for every lookup, using heuristic fallback.  
- **Implements:**  
  - `IDictionary`  
  - `IDictionary<TKey, TValue>` where `TKey : notnull` and `TValue : notnull`  
  - `IInsistent` / `IInsistentDictionary`  
- **Behavior:**  
  1. Attempts factory delegate provided via indexer.  
  2. Uses `DefaultActivationType` if available.  
  3. Reflects parameterless constructor if needed and caches the activator.  
- **Why it matters:** Ideal for self-hydrating caches and dependency registries that must always produce a value.

#### InsistentReturnDefaultEntry
An insistent variant that returns the heuristically created instance without committing it to the dictionary.  
- **Why it matters:** Grants the caller discretion to decide whether the value should persist.

___
### Brisk (Invariant and Non-Generic)
A **non-generic**, insistent dictionary that guarantees an `IDictionary` instance for any key.  
- **Behavior:**  
  - Keys can be primitive, type-based, or composite via `ComplexKey` patterns.  
  - Ensures every key resolves to a valid dictionary (never `null`).  
- **Why it matters:** Originally designed for **Type Exchange Abstraction (TEA)** (thus the name) reflection caching, Brisk enables lightweight contextual registries and composable lookup trees without type-checking ceremony.  
- **Indexer Signature:**
  ```csharp
  IDictionary this[object key, params object[] moreKeys] { get; }
  ```
- **Also Supports:**  
  - `[StdComplexKey]` enums that provide friendly names for complex key paths.  
  - Fast cache population and cross-type value mediation.

___


___


Below this line, the document  is under development and has the following provisional links.

[Brisk Signature Equality](./IVSoftware.Portable.Collections/README/brisk-signature-equality.md)


[Insistent Dictionary Pattern](./IVSoftware.Portable.Collections/README/insistent-dictionary-pattern.md)

[Tolerant Dictionary Pattern](./IVSoftware.Portable.Collections/README/tolerant-dictionary-pattern.md)

[Open Issues](./IVSoftware.Portable.Collections/README/open-issues.md)


The subreads on GitHub will be found at this root:

[(SAVE) SUBREADS](https://github.com/IVSoftware/IVSoftware.Portable.Collections/blob/master/IVSoftware.Portable.Collections/README)


