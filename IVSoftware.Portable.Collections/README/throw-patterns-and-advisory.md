## [<](../../README.md)

## The Three Tiers of Uncertainty

Error handling is not just about exceptions.  
It is about what the framework *believes* about the world at the moment something unexpected occurs.  
The **Three Tiers of Uncertainty** define how strongly that belief is held, and how much authority the framework yields to the End-User Developer (EUD).

Each tier represents a different degree of confidence that the current execution context can, or should, continue.

---

### 1. Throw Hard — "This world cannot continue."

A hard throw means the framework has lost logical cohesion.  
An invariant has broken, a critical assumption has failed, or a state transition would corrupt the model.  
This is ontological failure: the framework no longer believes its own reality.

**Intent:** Abort execution unless the EUD explicitly overrides.  
**Default:** Throws.  
**EUD role:** May intercept, suppress, or downgrade through the `Throw.BeginThrowOrAdvise` event.

```csharp
this.ThrowHard<InvalidOperationException>(
    "Type resolution failed for registered alias");
```

**Philosophy:**  
> "The laws of this universe are violated. I'm stopping before the fabric tears."

---

### 2. Throw Soft — "This did not go as planned, but it might still be livable."

A soft throw indicates epistemic uncertainty: the framework can still operate,  
but something deviated from expectation — a fallback path, a missing schema, or a failed heuristic.

The system prepares a valid `Exception` and surfaces it, but leaves the final judgment to the EUD.

**Intent:** Notify without forcing control flow to break.  
**Default:** Never throws.  
**EUD role:** Can log, ignore, or rethrow manually.

```csharp
var evt = this.ThrowSoft<InvalidCastException>(
    "Heuristic converter produced non-canonical result");

if (userPolicy.ThrowOnSoft)
    throw evt.Exception!;
```

**Philosophy:**  
> "I cannot vouch for this path anymore. You should probably take over."

---

### 3. Advisory — "Something noteworthy happened."

An advisory is not an error at all.  
It records an event of interest: a fallback, substitution, or optimization that changes behavior but not correctness.

**Intent:** Inform.  
**Default:** Writes to debug output when unhandled.  
**EUD role:** Can route to logging or telemetry; escalation is discouraged but possible.

```csharp
this.Advisory("Using default culture for value conversion");
```

**Philosophy:**  
> "You might want to know I did this — not because it is bad, but because it is interesting."

---

### Unified Signal Channel

All tiers raise through the same static event:

```csharp
Throw.BeginThrowOrAdvise += (_, e) =>
{
    switch (e)
    {
        case Advisory adv:
            Log.Info($"[Advisory] {adv.FormattedMessage}");
            break;
        case { ThrowRequestedAtCallSite: true } hard:
            Log.Error($"[ThrowHard] {hard.FormattedMessage}");
            break;
        default:
            Log.Warn($"[ThrowSoft] {e.FormattedMessage}");
            break;
    }
};
```

This single interception point keeps diagnostics uniform while letting consumers decide which signals are fatal, recoverable, or merely noteworthy.

---

#### Why It Matters

- **Clarity:** Every unexpected condition is visible and never silent.  
- **Tolerance:** Failure is not always fatal; uncertainty is a first-class state.  
- **Sovereignty:** The framework expresses its judgment, but the EUD makes the final call.

---

The Three Tiers of Uncertainty embody the design philosophy of *Type Exchange Abstraction*:
