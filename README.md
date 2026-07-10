# Unity Event Bus

A type-safe, **zero-allocation**, **reflection-free** static event bus for Unity, packaged as a
proper UPM package with tests. Written from scratch, inspired by the event bus pattern popularized
by [adammyhre/Unity-Event-Bus](https://github.com/adammyhre/Unity-Event-Bus) — same familiar
usage pattern, completely different internals.

## Why another event bus?

| Typical implementation (e.g. the original) | Problem | This version |
|---|---|---|
| `using UnityEditor;` outside `#if UNITY_EDITOR` | **Player builds fail to compile** | Editor code isolated in an Editor-only assembly |
| `PredefinedAssemblyUtil` scans only `Assembly-CSharp` / `-firstpass` | Events in **asmdef assemblies are never found**, so their buses are never cleared | No scanning at all — buses self-register via static constructor on first use |
| `Raise` copies bindings into a `new HashSet<>` every call | **GC allocation per raise** — bad in hot paths | Ordered list + deferred add/remove while raising: allocation-free |
| `GetMethod("Clear", NonPublic)` + `MakeGenericType` over all event types at startup | Fragile string-based reflection; eagerly initializes buses nobody uses | `EventBusRegistry` holds plain `Action` clear delegates; only touched buses register |
| `IEventBinding<T>` has public delegate **setters** | Any consumer can overwrite and drop everyone else's handlers | Interface is read-only; mutation only via `Add`/`Remove` on the owning binding |
| Loose scripts + sample events mixed into the library | Manual copy install, `TestEvent` ships to consumers | UPM package (`Runtime`/`Editor`/`Tests` + asmdefs), installable by git URL |
| `HashSet` iteration order | Non-deterministic handler order | Handlers run in registration order |

The runtime assembly has `noEngineReferences: true` — it is pure C#, so the core logic
compiles and unit-tests outside Unity too.

## Installation

**Package Manager (git URL)** — `Window ▸ Package Manager ▸ + ▸ Add package from git URL...`:

```
https://github.com/thinhtranbmt/Unity-Event-Bus.git
```

Or add to `Packages/manifest.json`:

```json
"com.thinhtranbmt.unity-event-bus": "https://github.com/thinhtranbmt/Unity-Event-Bus.git"
```

Requires Unity 2021.3+.

## Usage

Define an event (prefer a `readonly struct` — no boxing, no accidental mutation):

```csharp
using UnityEventBus;

public readonly struct PlayerDamaged : IEvent
{
    public readonly int Amount;
    public PlayerDamaged(int amount) => Amount = amount;
}
```

Subscribe and unsubscribe with a binding:

```csharp
using UnityEngine;
using UnityEventBus;

public class HealthUI : MonoBehaviour
{
    private EventBinding<PlayerDamaged> _binding;

    private void OnEnable()
    {
        _binding = new EventBinding<PlayerDamaged>(OnPlayerDamaged);
        EventBus<PlayerDamaged>.Register(_binding);
    }

    private void OnDisable()
    {
        EventBus<PlayerDamaged>.Deregister(_binding);
    }

    private void OnPlayerDamaged(PlayerDamaged e)
    {
        Debug.Log($"Player took {e.Amount} damage");
    }
}
```

Raise from anywhere:

```csharp
EventBus<PlayerDamaged>.Raise(new PlayerDamaged(10));
```

Extra handlers can be composed onto one binding:

```csharp
_binding.Add(() => Debug.Log("no-args listener on the same binding"));
```

## Semantics (explicit, and covered by tests)

- **Deregister during a raise**: the removed binding is *not* invoked for that raise.
- **Register during a raise**: the new binding is first invoked on the *next* raise.
- **Nested raises** (a handler raising the same event type) are supported.
- **Handler exceptions propagate** to the `Raise` caller; the bus stays in a consistent
  state. Wrap your handler body in `try/catch` if one listener must not break others.
- **Order**: handlers run in registration order.
- **Threading**: main-thread only, by design (it is a Unity event bus).
- **Play mode exit** clears every touched bus automatically (works with Domain Reload
  disabled), so stale listeners never leak between sessions.

## Diagnostics

```csharp
int listeners = EventBus<PlayerDamaged>.BindingCount;
IReadOnlyList<string> touched = EventBusRegistry.ActiveBusNames;
EventBusRegistry.ClearAll(); // e.g. between integration tests
```

## Tests

EditMode tests live in `Tests/Editor`. Run them via `Window ▸ General ▸ Test Runner`.

## License

MIT — see `LICENSE`.

## Inspired by

- [adammyhre/Unity-Event-Bus](https://github.com/adammyhre/Unity-Event-Bus) — [tutorial video](https://youtu.be/4_DTAnigmaQ)
