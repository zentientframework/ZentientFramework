// <copyright file="IRegistry{out TConcept}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Registries
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Zentient.Concepts;

    /// <summary>
    /// Minimal async-friendly registry contract.
    /// Implementations MUST avoid double-creation under concurrent access for <see cref="GetOrAddAsync"/>.
    /// Mutating operations surface recoverable outcomes via explicit result objects instead of throwing.
    /// </summary>
    /// <typeparam name="TConcept">The concept type stored in the registry.</typeparam>
    public interface IRegistry<TConcept> : IReadOnlyRegistry<TConcept>
        where TConcept : IConcept
    {
        /// <summary>
        /// Attempts to register a concept asynchronously. Returns a <see cref="RegistryResult"/> describing outcome.
        /// Implementations MUST NOT throw for recoverable id/name conflicts; such outcomes must be surfaced via the result.
        /// Exceptions are reserved for unrecoverable input errors (null/whitespace arguments, corrupt state).
        /// </summary>
        /// <param name="concept">The concept to register.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>A <see cref="RegistryResult"/> describing the outcome of the registration attempt.</returns>
        ValueTask<RegistryResult> TryRegisterAsync(TConcept concept, CancellationToken token = default);

        /// <summary>
        /// Attempts to remove an item with the specified id. Returns a <see cref="RegistryRemoveResult"/> describing outcome.
        /// Implementations MUST NOT throw for the item-not-found case; return a non-success result instead.
        /// </summary>
        /// <param name="id">The stable identifier of the item to remove.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>A <see cref="RegistryRemoveResult"/> describing the outcome of the removal attempt.</returns>
        ValueTask<RegistryRemoveResult> TryRemoveAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Atomically get-or-add by <paramref name="id"/> using the provided asynchronous <paramref name="factory"/>.
        /// Implementations MUST avoid double-creation when called concurrently for the same <paramref name="id"/>.
        /// Factory failures are considered unrecoverable and may throw.
        /// </summary>
        /// <param name="id">The stable identifier of the item to get or add.</param>
        /// <param name="factory">Asynchronous factory function to create the item if it does not already exist.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>The existing or newly created item.</returns>
        ValueTask<TConcept> GetOrAddAsync(string id, Func<CancellationToken, Task<TConcept>> factory, CancellationToken token = default);

        /// <summary>
        /// Attempts to find an element that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that defines the conditions of the element to search for. Cannot be null.</param>
        /// <param name="item">When this method returns, contains the first element that matches the predicate, if found; otherwise, the
        /// default value for the type of the element.</param>
        /// <returns>true if an element that matches the predicate is found; otherwise, false.</returns>
        bool TryGetByPredicate(Func<TConcept, bool> predicate, out TConcept? item);
    }
}
