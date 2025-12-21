## IVSoftware.Portable.Collections

A first **Collections** release introducing three *opinionated* dictionary types that emphasize contract-based lookup semantics. Originally developed to support **Type Exchange Abstraction (TEA)** interoperability (slated for release in early 2026 as a separate NuGet package), their usefulness extends well beyond TEA — providing predictable, expressive behaviors for everyday collection design.

## Dictionary Personality Summary

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
#### TolerantCreateNullEntry
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


Below this line, the document  is under development and has the following provisional links.

[Brisk Signature Equality](./IVSoftware.Portable.Collections/README/brisk-signature-equality.md)


[Insistent Dictionary Pattern](./IVSoftware.Portable.Collections/README/insistent-dictionary-pattern.md)

[Tolerant Dictionary Pattern](./IVSoftware.Portable.Collections/README/tolerant-dictionary-pattern.md)

[Open Issues](./IVSoftware.Portable.Collections/README/open-issues.md)


The subreads on GitHub will be found at this root:

[(SAVE) SUBREADS](https://github.com/IVSoftware/IVSoftware.Portable.Collections/blob/master/IVSoftware.Portable.Collections/README)


