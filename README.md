# ALPHA PRERELEASE
___



## IVSoftware.Portable.Collections [GitHub](https://github.com/IVSoftware/IVSoftware.Portable.Collections.git)


Preview-stage `INotifyCollectionChanging` semantics with mutable event args for cancellation or modification before changes occur. Its event constructors follow the familiar BCL `NotifyCollectionChangedEventArgs` syntax, and the `CopyToChangedEvent` method cleanly reconnects the preview stage to the conventional Changed-event flow.

___

### Inversion of Control

The preview system transforms collection mutation into a three-phase pipeline:

1. Each collection method emits a *proposal* in the form of a `NotifyCollectionChangingEventArgs` instance.  
2. Handlers inspect, cancel, or rewrite the proposal while it is still mutable.  
3. The collection delegates authority to the finalized proposal, applying the changes exactly as described through a single `ApplyChanges` method.

This is not a negotiation. It is a disciplined rewrite stage, similar in spirit to a compiler pass: intent enters, a transformed and validated version exits. The result is a predictable and safe model for observing and shaping collection changes before they take effect.

___

### Highlights

- Raise collection-change notifications *before* they occur, with full opportunity to validate or modify the operation.
- Use pattern matching as a natural alternative to `TryGetValue` when working with `TolerantDictionary` variants.
- Guarantee on-demand activation of missing keys through `InsistentDictionary` semantics.
- Unify access to layered or multi-source caches with `BriskDictionary`.
- Reduce UI churn through preview-aware range handling.
___

## What this Package Contains

In addition to the preview-stage event model (`INotifyCollectionChanging` and `NotifyCollectionChangingEventArgs`), this package includes two canonical collection types: `ObservablePreviewCollection<T>` and `ObservableDictionary<TKey,TValue>`. It also provides three specialized dictionary variants, each with a distinct behavioral "personality" suited to common .NET problem domains.

___

## Table of Contents

| # | Included in this Package | Description |
|---|------------------|---------------------|
| 1 | [NotifyCollectionChangingEventArgs](#notifycollectionchangingeventargs) | A `CancelEventArgs` derived instance whose mutable properties become the authority for what changes will occur. |
| 2 | [`ObservablePreviewCollection<T>`](#observablepreviewcollection) | An `ObservableCollection<T>` derived type that raises a preview event before applying changes, with support for range operations, distinct-item enforcement, and an optional O(1) Contains optimization.|
| 3 | [`ObservableDictionary<TKey,TValue>`](#observabledictionary) |A preview-aware implementation of `IDictionary` that fully supports both `INotifyCollectionChanging` and `INotifyCollectionChanged`, representing key–value proposals through `DictionaryEntryPreview` instances.|
| 4 | [`TolerantDictionary<TKey,TValue>`](#tolerantdictionary) | Provides a pattern-matching alternative to `TryGetValue` where preview semantics allow missing keys to be remedied, returned as benign nulls, or explicitly cached as intentional null misses. |
| 5 | [`InsistentDictionary<TKey,TValue>`](#insistentdictionary) | A dictionary that insists upon returning non-null values by activating missing entries through a fallback pipeline, with preview semantics giving handlers the final say on the value before it is applied. |
| 6 | [BriskDictionary](#briskdictionary) | A non-generic, multi-key `IDictionary` that uses insistent-preview semantics to always return a dedicated `Dictionary<object,object>` for each key path, which may be optionally upgraded to a strongly typed `IObservableDictionary<TKey,TValue>` on demand. |

___
## NotifyCollectionChangingEventArgs

Developers working with XAML-based frameworks (WPF, MAUI, WinUI, Xamarin.Forms) routinely rely on `INotifyCollectionChanged` without needing to examine its event args. It just works (IJW). `NotifyCollectionChangingEventArgs` plays a similar role here. The specialized IJW collections provided by this package subscribe to it automatically, so most End User Developers (EUDs) never need to engage with it directly.

Rather than dissect the type in isolation, it is more intuitive to look at how the specialized collections make use of it:

- **ObservablePreviewCollection** consults it to anticipate distinctness and offer a cancellation point before adding the item.
- **TolerantDictionary** raises it before returning a missing key as a benign null, giving handlers the chance to supply a value or explicitly cache a null miss.
- **InsistentDictionary** uses it to detect that a required value is not present so it can activate and insert a new instance before returning the contracted non-null value.
- **BriskDictionary** gives the EUD a final opportunity to weigh in on dictionary activations, attach event subscriptions, or substitute an entirely different dictionary type for the proposed one.

A simple and representative scenario involves reflection caching for an arbitrary `Type`. For example:

1. Attempt to obtain `PropertyInfo` for a member named `IsVisible`.  
2. Cache the reflected value when found.  
3. If not found (for example, on WinForms `Control`), cache the miss so the absence can be detected in O(1) time on subsequent lookups.

This snippet contains a forward reference to `TolerantDictionary` but crisply illustrates why handling the event directly can be advantageous.
___

### Example - Reflection Caching (successful) with Pattern Matching

This snippet uses .NET MAUI to show how an unknown object can be probed once, with future reflection requests amortized to O(1). In a tolerant or insistent dictionary, the indexer getter raises a `Replace` preview event when a key is not found. Conceptually, this appears as the `OldItem` having a null `Key` (the canonical tell), while the `NewItem` contains the missing `Key` (in this case, "IsVisible") and a null `Value`. When the handler supplies a non-null `Value`, the dictionary adds it to the collection for fast retrieval next time.

```
[TestMethod]
public void Test_ReflectionCachingWhenFound()
{
    // Evaluate reflection on an unknown type;
    object unk = new Microsoft.Maui.Controls.Button();

    var cache = new TolerantDictionary<string, PropertyInfo>();

    cache.CollectionChanging += OnCollectionChanging;

    // Local function expresses familiar handler 'shape'.
    void OnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
    {
        if (sender is IDictionary && sender is ITolerant)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangingAction.Replace:
                    if (e.GetNewItemSingle() is DictionaryEntryPreview entry &&
                        entry.Key is string key &&
                        entry.Value is null)
                    {
                        entry.Value = unk.GetType().GetProperty(key);
                    }
                    break;
            }
        }
    }

    // TEST BEGINS HERE
    PropertyInfo? pi = cache["IsVisible"];
    Assert.IsTrue(pi is PropertyInfo, "Expecting an instance of PropertyInfo.");
    Assert.IsTrue(cache.ContainsKey("IsVisible"), "Expecting O(1) retrieval in future calls.");
}
```

___

## ObservablePreviewCollection<T>

This implementer of `INotifyCollectionChanging` is a drop-in replacement for `ObservableCollection<T>`. 

It has been exhaustively tested for compatibility, including constrained random mutation for edge cases, in a manner that is on public display in the companion [GitHub Repo](https://github.com/IVSoftware/IVSoftware.Portable.Collections.git). This codebase includes the source code for the collection itself, along with the MSTest project on which we base this claim.

Its advanced features are backed by a surprisingly simple theory of operation:

- Every method (including the non-generic `IList` implementation) that results in an add, remove, move, reset (clear), or replace (by way of [Indexer].set) is considered a _proposal_ and is drawn up as a preview event before invoking the `CollectionChanging` event.
- Upon returning from the handler, if the `Cancel` property has not been set, it proceeds to the `ApplyChanges` method that faithfully executes whatever is in the event _without any regard for the method that initiated it._
- In short, the EUD who handles the `CollectionChanging` event is a superuser with great power and great responsibility.

All sanctioned mutations flow through NotifyCollectionChangingEventArgs; intentional misuse by direct modification through the base class API is possible but unsupported.

See also: [OPC Advanced Features](./IVSoftware.Portable.Collections/README/opc-advanced.md)

___

## ObservableDictionary

This class implements the following:

- `IDictionary`
- `IDictionary<TKey, TValue> where TKey : notnull`
- `INotifyCollectionChanging`
- `INotifyCollectionChanged`

It deliberately _is not_ a subclass of `Dictionary<TKey, TValue>` by inheritance, and this avoids the risks of implementation leaks that would otherwise surface.

But it _is_ the base class for `TolerantDictionary` and `InsistentDictionary`.

### The Difference, Explained.

Observability is mainly achieved through the indexed setter:

```

[Indexer]
public virtual TValue? this[TKey key]
{
    get => @base[key];
    set
    {
        NotifyCollectionChangingEventArgs ePre;
        if (@base.TryGetValue(key, out var oldValue))
        {
            ePre = new NotifyCollectionChangingEventArgs(
                action: NotifyCollectionChangingAction.Replace,
                newItem: new DictionaryEntryPreview(key, value),
                oldItem: new DictionaryEntryPreview(key, oldValue));
        }
        else
        {
            ePre = new NotifyCollectionChangingEventArgs(
                action: NotifyCollectionChangingAction.Add,
                changedItem: new DictionaryEntryPreview(key, value));
        }
        OnCollectionChanging(ePre);
        if (ePre.Cancel)
        {
            this.ThrowSoft<OperationCanceledException>();
        }
    }
}
```

But in terms of indexed getter it's just a passive draw on the dictionary that is the backing store.

```
    get => @base[key];
```

As you will see below, the indexed getter of tolerant and insistent personalities is dynamic and in the case of insistent is also heuristic.
___

## TolerantDictionary

One reason to use `TolerantDictionary` is for its support of pattern matching semantics:

```
    if(cache["IsVisible"] is PropertyInfo pi)
    {
        pi.SetValue(this, "true");
    }
```

However, it offers more than a sprinkle of syntactic sugar over what would otherwise be a trivial `TryGetValue` call. The first example showed "successful" reflection caching using the preview capability, but went on to add that returning a _non-null value_ instructs the sender to add it to the collection. And yet, we know that the probe is going to return null if `unk` is a WinForms `Button` and we try to retrieve `PropertyInfo` for "IsVisible".

On its face, this suggests that a _successful_ probe becomes O(1) in the future, but that a null result means we're doomed to repeat the probe repeatedly with the same null result every time. Fortunately, for this scenario all one needs to do is assign the `TolerantValue.ExplicitNull` enum instead of `null` and this provides the signal that `null` should be _added_ to a collection that would otherwise passively return benign null.


### Example - Reflection Caching (unsuccessful) with Cached Miss Mentality

This snippet uses .NET MAUI to show how an unknown object can be probed once, with future reflection requests amortized to O(1) _even if the reflected property is not found_.

```
[TestMethod]
public void Test_ReflectionCachingWhenNotFound()
{
    object unk = new Microsoft.Maui.Controls.Button();
    int probeCount = 0;

    var cache = new TolerantDictionary<string, PropertyInfo>();
    cache.CollectionChanging += OnCollectionChanging;

    void OnCollectionChanging(object? sender, NotifyCollectionChangingEventArgs e)
    {
        if (sender is IDictionary && sender is ITolerant)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangingAction.Replace:
                    if (e.GetNewItemSingle() is DictionaryEntryPreview entry &&
                        entry.Key is string key &&
                        entry.Value is null)
                    {
                        var pi = unk.GetType().GetProperty(key);
                        if (pi is null)
                        {
                            entry.Value = TolerantValue.ExplicitNull;
                        }
                        else
                        {
                            entry.Value = pi;
                        }
                        probeCount++;
                    }
                    break;
            }
        }
    }

    // TEST BEGINS HERE
    // Premise: The WinForms version of "IsVisible" is *not* supported on the MAUI Button.
    PropertyInfo? pi = cache["Visible"];

    Assert.IsTrue(pi is null, "Expecting PropertyInfo not found.");
    Assert.IsTrue(cache.ContainsKey("Visible"), "Expecting O(1) retrieval in future calls.");
    Assert.IsTrue(probeCount == 1, "Expecting probeCount is incremented by event handler.");

    // VERIFY THAT NULL RETRIEVAL IS O(1)
    pi = cache["Visible"];
    Assert.IsTrue(probeCount == 1, "Expecting probeCount is still 1 because key is found.");
}
```

___

## InsistentDictionary

So far, here's what we know about dictionary behavior when the indexer is called with a non-existent key:


| Class | Behavior |
|------------------|---------------------|
|`Dictionary<TKey,TValue>`| Throws `KeyNotFoundException`  |
|`ObservableDictionary<TKey,TValue>` | Throws `KeyNotFoundException`  |
|`TolerantDictionary<TKey,TValue>` | Returns benign null. Remedial options are offered in preview stage.  |

Next we intoduce Insistent behavior, which starts out as Tolerant in terms of preview handling for a missing key. The difference is that this remedial phase is contractually obligated to produce a non-null value to go along with any new ad hoc key. To emphasize this, an insistent dictionary attaches an additional `notnull` constraint to `TValue` (not just `TKey`).

If, after the preview phase, the resolved value is still `null`, the insistent contract has been violated. That failure mode is uncommon in practice because the heuristic pipeline usually resolves a value before the event is even raised, making the handler stage more of a review than a rescue.

Here, from highest to lowest priority, are the steps taken to obtain an instance prior to raising the event:

1. EUD may set the ActivationDlgt property — a delegate that produces an instance of `TValue`. This carries the highest priority when present.
2. If `TValue` is non-abstract and has a parameterless constructor, it becomes eligible for default activation.
3. If `TValue` is an interface, the framework checks for `[UnilateralContractAttribute]` to identify a default concrete implementation.

If any of these steps succeed, the preview value is offered to the handler to allow or veto (by cancellation or replacement). Otherwise, the handler _must_ supply an instance to avoid the contract violation.
___


## BriskDictionary

This personality is a non-generic insistent implementer of `IDictionary<object, IDictionary>`. As a precursor to a reflection-intensive interop pattern named Type Exchange Abstraction (TEA), it earned the "brisk" moniker by unifying many small reflection caches behind a single lookup surface, for example, allowing `PropertyInfo`, `MethodInfo`, `ConstructorInfo`, and `EventInfo` to become O(1) lookups after the initial reflection cost, making it quick to refresh.

Described in this manner, it may sound highly specialized - almost niche. But if we peel back the "caching" premise, the simple underlying truth has broader implications and fits far more use cases than reflection alone. The following snippet gives Brisk a single key (in this example, `typeof(PropertyInfo)`). In response, Brisk returns a `Dictionary<object,object>` that we are free to use however we like:

```csharp
var brisk = new BriskDictionary();

// Returns a "dictionary unknown" — a.k.a. dunk
IDictionary dunk = brisk[typeof(PropertyInfo)];

dunk["IsVisible"] = unk.GetType().GetProperty("IsVisible");

// Incidentally, 'dunk' is Tolerant, so pattern matching is OK.
if (dunk["IsVisible"] is PropertyInfo pi)
{
    pi.SetValue(unk, true); // Using the reflected PropertyInfo
}
```
Developers who have done this kind of work may already be shifting in their seats: the moment you cache *one* thing, you realize you actually need to cache *many* things. This is why reflection makes such an effective example — it exposes the shape of the problem immediately.

`PropertyInfo` alone isn't enough. You also need `MethodInfo`, `ConstructorInfo`, `EventInfo`, and sometimes type-level metadata. And that's just for *one* runtime type. A well-intentioned "just cache the reflection results" can quickly turn into a sprawl of small dictionaries.

Brisk addresses this pressure point directly by letting you organize those caches without ever having to *manage* them. And this becomes clear the moment we look at its defining feature:

### The Multi-Key Indexer

Most dictionaries have an indexer like this:

```csharp
public TValue this[TKey key] { ... }
```

Brisk's indexer looks like this:

```csharp
public IObservableDictionary this[object key, params object[] moreKeys] { ... }
```

In plain language, this means "any number of keys, of any type." And notice what happens the moment we supply just one extra term:

```csharp
dunk = brisk[unk.GetType(), typeof(PropertyInfo)];
```

Now, let's put a finer point on the statement that Brisk always returns `IDictionary`, because:

- The `dunk` is an insulated, dedicated dictionary. It isn't nested or stored inside any other dictionary.
- Yet it's aware of its own key path and is uniquely indexed by the runtime type of `unk`.

The key insight is that this expression is evaluated against the *current* runtime type of `unk`. The indexer does not rely on a predefined table of types; it resolves the key path on demand. If the runtime type of `unk` changes, the lookup naturally resolves to a different dictionary. This holds true whether you encounter ten types or a million of them.

As a natural consequence of Brisk's insistent personality, there is no need to predeclare, initialize, or manage these dictionaries.

___

### Type Safety

Earlier we obtained a `dunk` on demand and used it like this:

```csharp
if (dunk["IsVisible"] is PropertyInfo pi)
{
    pi.SetValue(unk, true);
}
```

This demonstrates Brisk's default stance: you get a flexible `Dictionary<object,object>` and rely on pattern matching to recover type information cleanly. For many scenarios — especially exploratory or reflection-driven ones — that is entirely sufficient. No setup, no declarations, no ceremony.

But some developers prefer stronger guarantees. If a particular dictionary should *only* ever contain `PropertyInfo` under string keys, Brisk will let you express that intention explicitly. It does not require initialization, but it willingly accepts it when you want tighter contracts. That is where `AsStronglyTypedDictionary` comes in.

```csharp
[TestMethod]
public void Test_BriskTypeSafety()
{
    object unk = new Microsoft.Maui.Controls.Button();

    var brisk = new BriskDictionary();

    // Install strongly typed reflection caches for this unknown type.
    _ = brisk[unk.GetType(), typeof(ConstructorInfo)].AsStronglyTypedDictionary<string, ConstructorInfo>();
    _ = brisk[unk.GetType(), typeof(PropertyInfo)].AsStronglyTypedDictionary<string, PropertyInfo>();
    _ = brisk[unk.GetType(), typeof(MethodInfo)].AsStronglyTypedDictionary<string, MethodInfo>();
    _ = brisk[unk.GetType(), typeof(EventInfo)].AsStronglyTypedDictionary<string, EventInfo>();

    // Retrieve the property cache and validate the strong type.
    var dunk = brisk[unk.GetType(), typeof(PropertyInfo)];
    Assert.IsInstanceOfType<TolerantDictionary<string, PropertyInfo>>(dunk);
}
```

Once upgraded, attempts to write a `MethodInfo` into the `PropertyInfo` dictionary will surface a preview-stage exception, preventing incorrect assignments. One point of caution: the Brisk indexer still returns `IDictionary` statically, even when the underlying dictionary has been upgraded. Pattern matching remains the preferred way to retrieve values safely and idiomatically.

___

## Ancestry

> "All problems in computer science can be solved by another level of indirection." — David Wheeler

A natural question arises:

**Q:** If I write `brisk[unk.GetType(), typeof(PropertyInfo)]`, is that the same as `brisk[unk.GetType()][typeof(PropertyInfo)]`?  
**A:** In other words, does a multi-key lookup imply that the second dictionary lives *inside* the first?

The short answer is **no** — the two expressions do **not** produce the same dictionary, and no containment relationship exists between them.

### Why They Are Not the Same

Calling:

```csharp
brisk[unk.GetType()]
```

returns a dedicated dictionary indexed by a *one-term* key path. That dictionary can later be upgraded into an arbitrary strongly typed dictionary, so it cannot safely serve as a container for other dictionaries. Therefore, the second lookup:

```csharp
brisk[unk.GetType()][typeof(PropertyInfo)]
```

would be attempting to index **into a dictionary that has no structural connection** to the multi-key lookup. The multi-key form does **not** decompose into nested dictionary traversal.

### So Why Does It *Feel* Hierarchical?

Because multi-key lookups do encode a *path* — not through nested dictionaries, but through **Brisk itself**. Brisk treats each key sequence as a standalone identity:

```csharp
// Equivalent identities:
brisk[ A, B ]   // key path: [A, B]
brisk[ A ][ B ] // two unrelated paths: [A] and then [B]
```

The dictionary you get from `brisk[unk.GetType(), typeof(PropertyInfo)]` is *aware* of its key path. Brisk records that path internally, and extensions like:

```csharp
var parentType = dict.Ancestors().OfType<Type>().FirstOrDefault();
```

let you retrieve the *leading* parts of that path — not because the dictionary is nested, but because Brisk exposes the ancestry metadata.

### The Right Mental Model

Think of Brisk as:

- a **router**, not a folder hierarchy,  
- giving you **dedicated sibling dictionaries**, not parent/child containers,  
- each keyed by the **entire path**, not by incremental indexing.

So while the expressions *look* similar, they are semantically distinct:

```
brisk[unk.GetType(), typeof(PropertyInfo)]   // one dictionary for the full path
brisk[unk.GetType()][typeof(PropertyInfo)]   // two unrelated dictionary resolutions
```

Understanding this distinction sets the stage for something more interesting:  
key ancestry is not trivia. It is what enables Brisk's cascading on-demand behavior.

___
## The Big Picture

Once a dictionary knows the key path that created it, a natural question arises:

___  
Q: Do I have to write handlers for these preview events?  
A: Not at all. But if you choose to, Brisk provides an unusually powerful vantage point.

The `dunk` returned by Brisk is not just an `IDictionary`; it is an observable dictionary that carries its own key ancestry. This means a handler can determine where in the Brisk universe this dictionary lives and tailor its behavior accordingly.

Consider the expression:

```csharp
brisk[unk.GetType(), typeof(PropertyInfo)]["IsVisible"]
```

Seen through the lens of ancestry, this becomes a pipeline:

1. The outer Brisk indexer identifies the reflection cache for the runtime type.  
2. The inner tolerant dictionary activates when a property key is missing.  
3. A handler, if attached, can walk the ancestry to understand what the lookup is really asking for.

This enables a cascading on-demand effect, where a missing entry prompts the system to reflect, store, and serve the result as though it had been present all along.

Here is the canonical initialization pattern:

```csharp
// One-time setup for the PropertyInfo cache of unk's runtime type.
_ =
    brisk[unk.GetType(), typeof(PropertyInfo)]
    .AsStronglyTypedDictionary<string, PropertyInfo>()
    .WithCollectionChangingEvent(onCollectionChanging: (sender, e) =>
    {
        // Handler body shown in the next example.
    });
```

And here is the full demonstration of why ancestry matters:

```csharp
[TestMethod]
public void Test_WhenToHandle()
{
    object unk = new Microsoft.Maui.Controls.Button();

    var brisk = new BriskDictionary();

    // One-time init.
    _ =
        brisk[unk.GetType(), typeof(PropertyInfo)]
        .AsStronglyTypedDictionary<string, PropertyInfo>()
        .WithCollectionChangingEvent(onCollectionChanging: (sender, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangingAction.Replace:
                    if (e.GetNewItemSingle() is DictionaryEntryPreview entry &&
                        entry.Value is null)
                    {
                        // If sender is IDictionary then we can query its Brisk key (if present).
                        if (sender is IDictionary dict)
                        {
                            if (dict.Ancestors().OfType<Type>().FirstOrDefault() is { } parentType)
                            {
                                Assert.AreEqual(typeof(Microsoft.Maui.Controls.Button), parentType);
                                { }
                            }
                        }
                    }
                    break;
            }
        });

    // Cascading on-demand reflection.
    if (brisk[unk.GetType(), typeof(PropertyInfo)]["IsVisible"] is PropertyInfo pi)
    {
        pi.SetValue(unk, true);
    }
    else
    {
        this.ThrowSoft<NotSupportedException>(
            $"{unk.GetType().Name} does not support the 'IsVisible' property");
    }
}
```

We call this the Big Picture not only because it ties the core Brisk concepts together in a single view, but also because Brisk itself is built upon the concepts of insistence and tolerance established earlier in the package.

- Each instance of BriskDictionary acts as one unified lookup surface. From that surface, it dispenses standalone dictionaries, each one uniquely indexed by its key path.
- Unlike a conventional dictionary with a single key term, Brisk accepts any number of comma-delimited terms. Each term contributes to a composite key that is resolved on demand.
- These multi-term keys describe a logical hierarchy that may be traversed, but this must not be mistaken for a Russian Doll nesting of dictionaries. The dispensed dictionaries are independent objects; their relationship is defined by key ancestry, not containment. "Logical hierarchy" simply means that a dispensed dictionary can make internal decisions based on the overall composition of its complex key.
- Optional strong-typing of dispensed dictionaries offers a safety net, guarding against accidental misuse.

Seen from this angle, Brisk's behavior becomes straightforward: one lookup surface per Brisk instance, many key paths, each path yielding its own dedicated space.

___

## Appendix: About Exception Handling
[GitHub](https://github.com/IVSoftware/IVSoftware.Portable.Common.git)

Some examples in this README reference `ThrowHard`, `ThrowSoft`, and `Advisory` from the companion package **IVSoftware.Portable.Common**, which this package already depends on. Its role is to provide a unified signaling model for exceptions and diagnostics in **Release-mode NuGet packages**, giving the End User Developer (EUD) full authority over how faults and messages are interpreted.

All signaling flows through a static event, `Throw.BeginThrowOrAdvise`, which lets the EUD observe, log, suppress, or escalate conditions *before* control flow is determined.

### Signal Types

#### ThrowHard  
Represents a failure the framework does not expect to recover from.  
If unhandled by the EUD, execution will throw upon return.  
If the EUD **marks it handled**, the throw is suppressed and control continues.

This creates a key distinction from the BCL:

- `throw new InvalidOperationException()`  
  Always terminates flow. No return value is required because the compiler knows execution cannot continue.

- `this.ThrowHard<InvalidOperationException>()`  
  *Will* throw unless the EUD suppresses it. Because suppression is possible, **the compiler requires a legal return path**, even if that path is only theoretical.

#### ThrowSoft  
Represents a non-critical condition.  
It is automatically considered *handled* when raised and **never throws** unless the EUD explicitly un-handles and escalates it.  
This makes it suitable for Try-patterns, optional behaviors, and recoverable scenarios like benign `OperationCanceledException`.

#### Advisory  
A lightweight diagnostic message, conceptually similar to `Debug.WriteLine`, but available in Release mode.  
Advisories never affect control flow and are always safe to ignore unless the EUD chooses to log or react to them.

### Why this matters here

This signaling model allows the Collections package to surface detailed, structured diagnostics in Release builds without forcing a crash or requiring a Debug-only toolchain. The EUD remains in complete control of how failures and advisories are interpreted, logged, or suppressed.