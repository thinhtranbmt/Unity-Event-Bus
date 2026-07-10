# Changelog

## [1.0.0] - 2026-07-10

Initial release. Written from scratch, inspired by
[adammyhre/Unity-Event-Bus](https://github.com/adammyhre/Unity-Event-Bus); the notes below
describe what this implementation does differently from that reference design.

### Fixed (relative to the reference implementation)
- `using UnityEditor;` outside `#if UNITY_EDITOR` in `EventBusUtil.cs` broke player builds.
- Events declared inside assembly-definition (asmdef) assemblies were invisible to
  `PredefinedAssemblyUtil` (it only scanned `Assembly-CSharp` and `-firstpass`), so their
  buses were never cleared on play-mode exit.
- `IEventBinding<T>` exposed public delegate setters, letting any consumer overwrite (and
  silently drop) every other consumer's handlers.

### Changed
- Reflection + assembly-scanning bootstrap replaced by `EventBusRegistry`: each
  `EventBus<T>` self-registers from its static constructor on first use. No
  `MakeGenericType`, no string-based `GetMethod("Clear")`, no eager initialization of
  unused buses, works with any assembly layout.
- `Raise` is now allocation-free: the per-call `HashSet` snapshot is replaced by ordered
  list iteration with deferred add/remove while a raise is in flight. Registration order
  is now deterministic.
- Restructured as a UPM package (`Runtime`/`Editor`/`Tests` + asmdefs) installable via git URL.
- Runtime assembly has `noEngineReferences: true` — pure C#, engine-independent, and the
  `Debug.Log` spam on init/clear is gone.
- `Register`/`Deregister` accept `IEventBinding<T>` instead of the concrete class and
  ignore `null`; double registration of the same binding is a no-op.

### Added
- `EventBus<T>.BindingCount` and `EventBusRegistry.ActiveBusNames` for diagnostics.
- EditMode test suite covering raise, deregister-during-raise, register-during-raise,
  nested raises, throwing handlers, and registry clearing.
