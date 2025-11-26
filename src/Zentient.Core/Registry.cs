// <copyright file="Registry.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Zentient.Concepts;

    /// <summary>
    /// Provides factory methods for creating registry instances for managing concept objects in memory.
    /// </summary>
    /// <remarks>The Registry class supplies static methods to create new in-memory registries for types
    /// implementing IConcept. Registries created through this class are suitable for scenarios where persistence is not
    /// required and are typically used for testing, prototyping, or lightweight runtime management of concept
    /// instances. The returned registries are thread-safe and support observer and tracing integration if
    /// provided.</remarks>
    public static class Registry
    {
        /// <summary>
        /// Creates a new in-memory registry for storing and managing concept instances of the specified type.
        /// </summary>
        /// <typeparam name="TConcept">The type of concept to be stored in the registry. Must implement <see cref="IConcept"/>.</typeparam>
        /// <param name="observer">An optional observer that receives notifications about registry changes. If <see langword="null"/>, no
        /// notifications are sent.</param>
        /// <param name="traceSink">An optional trace sink used to record diagnostic or tracing information. If <see langword="null"/>, tracing
        /// is disabled.</param>
        /// <returns>An in-memory implementation of <see cref="IRegistry{T}"/> for managing concept instances.</returns>
        public static IRegistry<TConcept> NewInMemory<TConcept>(IRegistryObserver<TConcept>? observer = null, ITraceSink? traceSink = null)
            where TConcept : IConcept
            => new InMemoryRegistryImpl<TConcept>(observer, traceSink);

        /// <summary>
        /// Provides an in-memory implementation of the <see cref="IRegistry{T}"/> interface for managing concept instances by ID and name.
        /// </summary>
        /// <remarks>This implementation stores all registered items in memory and is suitable for
        /// scenarios where persistence is not required. All operations are thread-safe. Name lookups are
        /// case-insensitive, while ID lookups are case-sensitive. This class is intended for internal use and is not
        /// designed for distributed or persistent scenarios.</remarks>
        /// <typeparam name="TConcept">The type of concept managed by the registry. Must implement <see cref="IConcept"/>.</typeparam>
        [DebuggerDisplay("Registry<{typeof(T).Name}> (Count = {_byId.Count})")]
        internal sealed class InMemoryRegistryImpl<TConcept> : IRegistry<TConcept> where TConcept : IConcept
        {
            private readonly ConcurrentDictionary<string, TConcept> _byId = new(StringComparer.Ordinal);
            private readonly ConcurrentDictionary<string, string> _nameToId = new(StringComparer.OrdinalIgnoreCase);
            private readonly ConcurrentDictionary<string, Lazy<Task<TConcept>>> _inflight = new(StringComparer.Ordinal);
            private readonly object _mutationLock = new();
            private readonly IRegistryObserver<TConcept>? _observer;
            private readonly ITraceSink? _trace;

            /// <summary>
            /// Initializes a new instance of the InMemoryRegistryImpl class with the specified observer and trace sink.
            /// </summary>
            /// <param name="observer">An optional observer that receives notifications about registry changes. May be <see langword="null"/> if
            /// not required.</param>
            /// <param name="trace">An optional trace sink used to record diagnostic or tracing information. May be <see langword="null"/> if tracing is not
            /// needed.</param>
            public InMemoryRegistryImpl(IRegistryObserver<TConcept>? observer = null, ITraceSink? trace = null)
            {
                _observer = observer;
                _trace = trace;
            }

            /// <inheritdoc/>
            public bool TryGetById(string id, out TConcept? item)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
                return _byId.TryGetValue(id, out item);
            }

            /// <inheritdoc/>
            public bool TryGetById(string id, out TConcept? item, out string? reason)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
                reason = null;
                var found = _byId.TryGetValue(id, out item);
                if (!found) reason = $"No item found with id '{id}'.";
                return found;
            }

            /// <inheritdoc/>
            public bool TryGetByName(string name, out TConcept? item)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

                if (_nameToId.TryGetValue(name, out var id) && _byId.TryGetValue(id, out var direct))
                {
                    item = direct;
                    return true;
                }

                string? bestId = null;
                TConcept? bestMatch = default;

                foreach (TConcept v in _byId.Values)
                {
                    if (!string.Equals(v.DisplayName, name, StringComparison.OrdinalIgnoreCase)) continue;

                    if (bestId == null || string.CompareOrdinal(v.Key, bestId) < 0)
                    {
                        bestId = v.Key;
                        bestMatch = v;
                    }
                }

                if (bestMatch != null)
                {
                    item = bestMatch;
                    _nameToId[name] = bestId!;
                    return true;
                }

                item = default;
                return false;
            }

            /// <inheritdoc/>
            public bool TryGetByName(string name, out TConcept? item, out string? reason)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
                reason = null;

                if (_nameToId.TryGetValue(name, out var id) && _byId.TryGetValue(id, out var direct))
                {
                    item = direct;
                    return true;
                }

                item = _byId.Values.FirstOrDefault(v => string.Equals(v.DisplayName, name, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    reason = $"No item found with name '{name}'.";
                    return false;
                }

                _nameToId[name] = item.Key;
                reason = null;
                return true;
            }

            /// <inheritdoc/>
            public bool TryGetByPredicate(Func<TConcept, bool> predicate, out TConcept? item)
            {
                ArgumentNullException.ThrowIfNull(predicate);
                item = _byId.Values.FirstOrDefault(v => predicate(v));
                return item != null;
            }

            /// <inheritdoc/>
            public bool TryGetByPredicate(Func<TConcept, bool> predicate, out TConcept? item, out string? reason)
            {
                ArgumentNullException.ThrowIfNull(predicate);
                item = _byId.Values.FirstOrDefault(predicate);
                reason = item == null ? "No item found matching the specified predicate." : null;
                return item != null;
            }

            /// <inheritdoc/>
            IEnumerable<TConcept> IReadOnlyRegistry<TConcept>.ListAll() => _byId.Values.ToArray();

            /// <inheritdoc/>
            public ValueTask<RegistryResult> TryRegisterAsync(TConcept concept, CancellationToken token = default)
            {
                ArgumentNullException.ThrowIfNull(concept, nameof(concept));
                ArgumentException.ThrowIfNullOrWhiteSpace(concept.Key, nameof(concept.Key));
                ArgumentException.ThrowIfNullOrWhiteSpace(concept.DisplayName, nameof(concept.DisplayName));
                token.ThrowIfCancellationRequested();

                var added = _byId.TryAdd(concept.Key, concept);

                if (added)
                {
                    _nameToId.TryAdd(concept.DisplayName, concept.Key);

                    try
                    {
                        _observer?.OnRegistered(concept);
                    }
                    catch { /* best-effort */ }

                    return new ValueTask<RegistryResult>(RegistryResult.Success(true));
                }

                if (_byId.TryGetValue(concept.Key, out var existing))
                {
                    if (!string.Equals(existing.DisplayName, concept.DisplayName, StringComparison.Ordinal))
                    {
                        var reason = $"Registry conflict: id '{concept.Key}' already registered with a different name.";
                        return new ValueTask<RegistryResult>(RegistryResult.Failure(reason, Enumerable.Empty<string>()));
                    }

                    return new ValueTask<RegistryResult>(RegistryResult.Success(false));
                }

                return new ValueTask<RegistryResult>(RegistryResult.Failure("Unknown registry outcome"));
            }

            /// <inheritdoc/>
            public ValueTask<RegistryRemoveResult> TryRemoveAsync(string id, CancellationToken token = default)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
                token.ThrowIfCancellationRequested();

                if (_byId.TryRemove(id, out var removed))
                {
                    _nameToId.TryRemove(removed.DisplayName, out _);
                    try
                    {
                        _observer?.OnRemoved(id);
                    }
                    catch { /* best-effort */ }
                    return new ValueTask<RegistryRemoveResult>(new RegistryRemoveResult(true));
                }

                return new ValueTask<RegistryRemoveResult>(new RegistryRemoveResult(false, "Item not found"));
            }

            /// <inheritdoc/>
            public async ValueTask<TConcept> GetOrAddAsync(string id, Func<CancellationToken, Task<TConcept>> factory, CancellationToken token = default)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
                ArgumentNullException.ThrowIfNull(factory, nameof(factory));
                token.ThrowIfCancellationRequested();

                if (_byId.TryGetValue(id, out var exist))
                {
                    return exist;
                }

                var lazy = _inflight.GetOrAdd(id, _ => new Lazy<Task<TConcept>>(async () => await FactoryWrapperAsync(id, factory, token).ConfigureAwait(false), LazyThreadSafetyMode.ExecutionAndPublication));

                TConcept created;

                try
                {
                    created = await lazy.Value.ConfigureAwait(false);
                }
                catch
                {
                    _inflight.TryRemove(id, out _);
                    throw;
                }

                return created;
            }

            private async ValueTask<TConcept> FactoryWrapperAsync(string id, Func<CancellationToken, Task<TConcept>> factory, CancellationToken token)
            {
                IDisposable? scope = null;
                try
                {
                    scope = _trace?.Begin("registry.getoradd");
                    var created = await factory(token).ConfigureAwait(false);

                    lock (_mutationLock)
                    {
                        if (_byId.TryGetValue(id, out var existing))
                        {
                            return existing;
                        }

                        _byId[id] = created;
                        _nameToId[created.DisplayName] = created.Key;
                    }

                    try
                    {
                        _observer?.OnRegistered(created);
                    }
                    catch { /* best-effort */ }
                    return created;
                }
                finally
                {
                    _inflight.TryRemove(id, out _);
                    scope?.Dispose();
                }
            }
        }
    }
}
