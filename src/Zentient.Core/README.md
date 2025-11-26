# Zentient.Core

Zentient.Core provides the concrete implementations and runtime primitives that power the Zentient platform. The package intentionally keeps concrete types internal and exposes a small, stable public surface built for discoverability, ergonomics and long-term compatibility.

This README documents the responsibilities, core concepts, public APIs and common usage patterns for `Zentient.Core`.

## Goals

- Provide battle-tested internal implementations of core abstractions (registries, execution contexts, lifecycles, serialization helpers).
- Keep public API small and stable; allow internal refactors without breaking consumers.
- Favor ergonomics for authors of application code while maintaining thread-safety and predictable disposal/cancellation behavior.

## What this package contains

Key public-facing abstractions (implemented internally):

- `IConcept` / `Concept` — a lightweight description object representing a registered concept.
- `IIdentifiable` / `INamed` / `IDescribed` — small building blocks for identity, naming and description metadata.
- `IExecutionContext` / `Execution` — context object used when executing work; carries cancellation and ambient data.
- `ILifecycle` / `Lifecycles` / `ILifecycleState` — lifecycle state machine primitives used to represent and observe lifecycle transitions.
- `IProvider<T>` / `IRegistry<T>` / `IReadOnlyRegistry<T>` / `Registry` — registry abstractions and a concrete, thread-safe registry implementation.
- `RegistryResult` / `RegistryRemoveResult` — typed results produced by registry operations with consistent semantics.
- `ISerializer` / `ISerializerAsync` / `Serialization` — pluggable serialization helpers (sync & async) used by other core components.
- `ITraceSink` — lightweight tracing sink used by components for structured diagnostics.

Most concrete implementations are internal to the package. Public consumers interact with the small set of interfaces and factory helpers exposed by the Abstractions packages and static facades.

## Design principles

- Abstractions are small and focused. Implementations are internal so the contract packages don't leak implementation details.
- Immutability where appropriate (e.g., concept descriptions and registry results).
- Deterministic, well-documented semantics for operations such as `Register`, `Remove`, `Merge` and lifecycle transitions.
- Thread-safety: registry operations and lifecycle transitions are safe to call concurrently from multiple threads.
- Cancellation and disposal responsibilities are explicit; `IExecutionContext` carries cancellation tokens and disposal is well-defined.

## Quick start examples

### Registering and resolving concepts

The `Registry<TConcept>` implementation provides a thread-safe registry for concepts or providers. Use the `IRegistry<T>` / `IReadOnlyRegistry<T>` abstractions to program against.

```csharp
// create a registry for some concept type
var registry = new Registry<Concept>();

// create a concept and register
var concept = new Concept("my-concept", description: "Example concept");
var addResult = registry.Register(concept);
if (addResult.IsSuccess)
{
    Console.WriteLine($"Registered: {concept.Id}");
}

// look up by id
if (registry.TryGet(concept.Id, out var found))
{
    Console.WriteLine(found.Name);
}

// remove
var remove = registry.Remove(concept.Id);
if (remove.IsRemoved)
{
    Console.WriteLine("Removed");
}
```

Refer to `RegistryResult` and `RegistryRemoveResult` for the detailed shape of outcomes and debug-friendly `ToString()`/`DebuggerDisplay` implementations.

### Execution and cancellation

`Execution` / `IExecutionContext` is the standard context passed to background work. It provides a cancellation token and ambient data storage.

```csharp
await using var exec = Execution.Create(); // creates an execution context with a linked CancellationTokenSource
var ctx = exec.Context;

// pass ctx to providers / handlers; they should honor ctx.CancellationToken
await someProvider.ExecuteAsync(ctx);

// cancel
exec.Cancel();
```

The execution types are designed for predictable disposal and to avoid CTS races when linked cancellation tokens are used.

### Lifecycles and observers

`ILifecycle` provides a simple state machine. Use `Lifecycles` helpers to create common lifecycle implementations and `IRegistryObserver<T>` to observe changes.

```csharp
var lifecycle = Lifecycles.CreateManual();
lifecycle.TransitionTo(LifecycleState.Starting);
// attach observers
lifecycle.TransitionTo(LifecycleState.Running);
```

Observers receive concise, reasoned events suitable for logging and metrics.

### Serializers

`ISerializer` and `ISerializerAsync` are lightweight, pluggable contracts. Implementations can be provided by consumers or selected via DI.

```csharp
// sync
byte[] payload = serializer.Serialize(obj);
var obj2 = serializer.Deserialize<T>(payload);

// async
await serializerAsync.SerializeAsync(stream, obj, cancellationToken);
```

## Thread-safety and performance notes

- `Registry<T>` is safe for concurrent `Register`, `TryGet`, and `Remove` operations. It uses lock-free or fine-grained synchronization internally to avoid global contention where possible.
- `Execution` uses `LazyThreadSafetyMode.PublicationOnly` for inflight factory behavior to avoid caching faulted factories.
- Where appropriate the library avoids capturing `SynchronizationContext` (`ConfigureAwait(false)`) and prefers `Immutable` collections for snapshot-friendly operations.

## Error handling and diagnostics

- Try-style methods (for example `TryGet`, `TryRemove`) return typed result objects that include reason information and are safe to log.
- `RegistryResult` and `RegistryRemoveResult` include helpful `ToString()` and `DebuggerDisplay` implementations to aid debugging.
- `ITraceSink` is a low-overhead sink for structured diagnostic events; implementations should be cheap and non-blocking.

## Testing guidance

- Unit tests should target the public contract (interfaces and static facades) and avoid depending on internal concrete types.
- Mocks or fakes can be implemented against `IProvider<T>`, `IRegistry<T>` or `IExecutionContext` for isolated testing.

## Contribution and maintenance notes

- Keep public surface minimal. If adding a new API, prefer adding it to the Abstractions package first and expose internal implementations in `Zentient.Core`.
- When changing internal implementations, preserve observable semantics of the public interfaces to avoid breaking downstream packages.
- Add unit tests for concurrency scenarios, lifecycle transitions and serializer round-trips.

## Versioning and changelogs

This package follows semantic versioning. See `CHANGELOG.md` for recent changes and migration notes.

## License

Zentient Framework is MIT licensed. See the top-level `LICENSE` file for details.

---

For more detailed examples, refer to the `tests` projects in the repository which exercise the registry, execution and lifecycle primitives with real-world scenarios.

