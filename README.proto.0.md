## IVSoftware.Portable.Collections

A first **Collections** release introducing three *opinionated* dictionary types that emphasize contract-based lookup semantics. Originally developed to support **Type Exchange Abstraction (TEA)** interoperability (available as a separate NuGet package), their usefulness extends well beyond TEA — providing predictable, expressive behaviors for everyday collection design.

### Dictionary Personality Summary

| Personality | Behavior |
|--------------|-----------|
| **Tolerant** | Returns `null` for missing keys without complaint. |
| **Insistent** | "Insists on" returning a non-null value. |
| **Brisk** | An insistent dictionary featuring jagged multikeys, ideal for fast caching. |

Let's begin with the most approachable of the three — **TolerantDictionary** —  
which makes pattern matching against possibly-missing keys feel natural and penalty-free compared to the traditional `TryGetValue` approach.

```csharp
TolerantDictionary<string, object> dict = new();

if (dict["Hello"] is { } value)
{
    // Use value.
}
else
{
    // Simply return without penalty, or choose to add the new key now.
}
```
___

Next, the value (in brief) o the Insistent lies not only in its refusal to take "no" for an answer, but that it can heuristically attempt default construction for missing keys.

What _both_ have in common is the `ValueArbitrationRequested` event (with a companion static `ValueArbitrationRequestedPreview`) empowering the End User Developer as the final decider for either pattern. 










while the third is a unique abstraction - an Insistent Dictionary that always returns an IDictionary as a value, and is accessed using a key _chain_ instead of a standard key.

```
/// <summary>
/// - Insistently returns a scoped dictionary, dispensing new object-object dictionary on demand.
/// - Signatures can be preregistered to create strongly typed dictionaries instead.
/// - Otherwise, the ValueRequired event gives End User Developer (EUD) an opportunity
///   to provide a strongly typed version.
/// </summary>
[Indexer]
IDictionary this[object key, params object[] moreKeys] { get; }
```

---
#### Tolerant

`TolerantDictionary` is the simplest of the three, its main charter is to allow a sugar syntax where pattern matching can be employed without breaking stride in place of a `TryGetValue`.

```csharp
[Flags]
enum Contrived
{
    OptionFlagSet = 1,
}

// Set an option if present in the dictionary.
void TolerantExample()
{
    var tol = new TolerantDictionary<string, Enum>();

    bool localOptionFlagSet = false;
    if (tol[nameof(Contrived)] is { } option)
    {
        localOptionFlagSet = option.HasFlag(Contrived.OptionFlagSet);
    }
}
```
The tolerant variant can raise an event before a missing key is initialized. The event provides a chance to seed a value or explicitly decide that the lookup should resolve to `null`.

Handlers receive a `ValueRequestedEventArgs`, which models an *optional* request:  
- `Value` may be `null`.  
- `IsSet` distinguishes between *no handler* (`false`) and *handler explicitly set null* (`true`).

Declaring a dictionary as tolerant or inheriting from it also conveys intent that absence is not an error, but it can still be observed, logged, or filled if desired.
___


#### Insistent

`InsistentDictionary` takes a stronger position by insisting that every key resolve to a usable value.  
Remedies include the following:

- If the `TValue` type can be default constructed, or if it offers a public static `Default` or `Empty` member,  
  that value is preloaded automatically.  
- After preload, the `ValueRequired` event is raised.  
  The handler can review the default selection or substitute its own value.

If none of these approaches succeed, the package client is notified by way of a cancellable throw.

```csharp
void InsistentExample()
{
    var insist = new InsistentDictionary<string, PropertyInfo>();
    int reflectionCount = 0;

    insist.ValueRequired += (s, e) =>
    {
        reflectionCount++;
        if (e.Key is string propertyName)
        {
            e.Value = typeof(Button).GetProperty(propertyName);
        }
    };

    // First lookup: event fires and reflection occurs.
    var prop = insist[nameof(Button.Visible)];

    // Second lookup: uses cached value, no reflection.
    prop = insist[nameof(Button.Visible)];

    Debug.Assert(reflectionCount == 1);
}
```

By insisting on a non-null result, this pattern turns lazy initialization into a contract.  
Missing entries must be resolved deterministically, making cache behavior explicit and testable.
___


## Dictionaries

This package defines three dictionary types that cooperate with each other,  
each with its own distinct personality and behavioral contract.

### Personality Summary

| Personality | Behavior |
|--------------|-----------|
| **Tolerant** | Returns null for missing keys without complaint. |
| **Insistent** | Insists on returning a non-null value. |
| **Brisk** | An insistent dictionary featuring jagged multi-key scopes. |

---

#### Tolerant

`TolerantDictionary` is the simplest of the three.  
Its main charter is to allow a sugar syntax where pattern matching can be employed  
in place of a traditional `TryGetValue`, keeping control flow in-line and uncluttered.

```csharp
[Flags]
enum Contrived
{
    OptionFlagSet = 1,
}

// Set an option if present in the dictionary.
void TolerantExample()
{
    var tol = new TolerantDictionary<string, Enum>();

    bool localOptionFlagSet = false;
    if (tol[nameof(Contrived)] is { } option)
    {
        localOptionFlagSet = option.HasFlag(Contrived.OptionFlagSet);
    }
}
```

Like its insistent counterpart, the tolerant variant can raise an event before  
a missing key is initialized. The event provides a chance to seed a value or  
explicitly decide that the lookup should resolve to `null`.

Handlers receive a `ValueRequestedEventArgs`, which models an *optional* request:  
- `Value` may be `null`.  
- `IsSet` distinguishes between *no handler* (`false`) and *handler explicitly set null* (`true`).

This lets tolerant collections express intention: absence is not an error,  
but it can still be observed, logged, or filled if desired.
___

#### Insistent

`InsistentDictionary` takes a stronger position by insisting that every key resolve to a usable value.  
Remedies include the following:

- If the `TValue` type can be default constructed, or if it offers a public static `Default` or `Empty` member,  
  that value is preloaded automatically.  
- After preload, the `ValueRequired` event is raised.  
  The handler can review the default selection or substitute its own value.

If none of these approaches succeed, the package client is notified by way of a cancellable throw.

```csharp
void InsistentExample()
{
    var insist = new InsistentDictionary<string, PropertyInfo>();
    int reflectionCount = 0;

    insist.ValueRequired += (s, e) =>
    {
        reflectionCount++;
        if (e.Key is string propertyName)
        {
            e.Value = typeof(Button).GetProperty(propertyName);
        }
    };

    // First lookup: event fires and reflection occurs.
    var prop = insist[nameof(Button.Visible)];

    // Second lookup: uses cached value, no reflection.
    prop = insist[nameof(Button.Visible)];

    Debug.Assert(reflectionCount == 1);
}
```

By insisting on a non-null result, this pattern turns lazy initialization into a contract.  
Missing entries must be resolved deterministically, making cache behavior explicit and testable.
___

#### Brisk

**Solves**
Management of Dictionaries that contain other Dictionaries.

`BriskDictionary` 

Each lookup resolves a `BriskSignature`, which normalizes a supplied key chain  
into a deterministic sequence of types, strings, and values.  
The result is an `IDictionary` representing that scope.

- If the scope exists, it is returned immediately.  
- If not, a new dictionary is created on demand.  
- Pre-registered signatures yield strongly typed dictionaries and do not raise `ValueRequired`.  
- Ad-hoc signatures raise `ValueRequired`, giving the consumer a chance to supply a strongly typed scope.

```csharp
void BriskExample()
{
    IBriskDictionary brisk = new BriskDictionary();

    // Retrieve or create a scoped dictionary for Button -> PropertyInfo.
    var scope = brisk[typeof(Button), typeof(PropertyInfo)];

    // Use the scope as a normal dictionary.
    scope[nameof(Button.Visible)] = typeof(Button).GetProperty(nameof(Button.Visible));
}
```

Where `Tolerant` accepts absence and `Insistent` resolves it,  
`Brisk` organizes it — binding values to a contextual hierarchy of types or instances.

This makes it ideal for reflection registries, property binders, and per-instance configuration models  
where multiple dictionaries share a common semantic root.

___
_As a forward reference, `BriskDictionary` serves as a precursor to the
Type Exchange Abstraction (TEA) pattern, Unilateral Contracts, and "Vibe Subclassing".
In short, it supports scenarios where the goal is to map (and especially to cache)
complex type relationships briskly, by meaning rather than by syntax,
and to make cross-type lookups fast, declarative, and semantically clear._
___


### Personality Gradient

| Personality | Core Idea | Common Use |
|--------------|------------|-------------|
| **Tolerant** | Absence is acceptable. | Optional settings, loose maps. |
| **Insistent** | Absence must be resolved. | Reflection caches, dependency tables. |
| **Brisk** | Jagged context caching | Support TEA pattern and "Vibe Subclassing" |



[Brisk Signature Equality](./IVSoftware.Portable.Collections/README/brisk-signature-equality.md)







[Insistent Dictionary Pattern](./IVSoftware.Portable.Collections/README/insistent-dictionary-pattern.md)

[Tolerant Dictionary Pattern](./IVSoftware.Portable.Collections/README/tolerant-dictionary-pattern.md)

[Open Issues](./IVSoftware.Portable.Collections/README/open-issues.md)




The subreads on GitHub will be found at this root:

[(SAVE) SUBREADS](https://github.com/IVSoftware/IVSoftware.Portable.Collections/blob/master/IVSoftware.Portable.Collections/README)


