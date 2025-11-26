// <copyright file="IReadOnlyRegistry.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.Collections.Generic;
    using Zentient.Concepts;

    /// <summary>
    /// Read-only registry contract. Provides deterministic lookup by id and by name.
    /// The name lookup will return a deterministic first-match; higher-level registries
    /// may impose stricter uniqueness constraints.
    /// </summary>
    /// <typeparam name="TConcept">The concept type stored in the registry.</typeparam>
    public interface IReadOnlyRegistry<TConcept>
        where TConcept : IConcept
    {
        /// <summary>
        /// Attempts to retrieve an item of type T by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the item to retrieve. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="item">When this method returns, contains the item associated with the specified identifier, if found; otherwise,
        /// the default value for type T.</param>
        /// <returns><see langword="true"/> if an item with the specified identifier was found; otherwise, <see langword="false"/>.</returns>
        bool TryGetById(string id, out TConcept? item);

        /// <summary>
        /// Attempts to resolve an item by its stable identifier.
        /// Throws only for unrecoverable input errors (null/whitespace).
        /// </summary>
        /// <param name="id">The stable identifier to lookup.</param>
        /// <param name="item">When this method returns, contains the resolved item if found; otherwise <see langword="null"/>.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if the item was not found; otherwise 
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if an item with the specified id was found; otherwise <see langword="false"/>.</returns>
        bool TryGetById(string id, out TConcept? item, out string? reason);

        /// <summary>
        /// Attempts to resolve an item by name. Returns a deterministic first-match when multiple items share a name.
        /// Throws only for unrecoverable input errors (null/whitespace).
        /// </summary>
        /// <param name="name">The name label to lookup.</param>
        /// <param name="item">When this method returns, contains the resolved item if found; otherwise <see langword="null"/>.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if the item was not found; otherwise
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if an item with the specified name was found; otherwise <see langword="false"/>.</returns>
        bool TryGetByName(string name, out TConcept? item, out string? reason);

        /// <summary>
        /// Attempts to find an element that matches the specified predicate.
        /// Throws only when <paramref name="predicate"/> is null.
        /// </summary>
        /// <param name="predicate">A function that defines the conditions of the element to search for. Cannot be <see langword="null"/>.</param>
        /// <param name="item">When this method returns, contains the found item if a match was found; otherwise, <see langword="null"/>.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if no matching item was found; otherwise, 
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching item was found; otherwise, <see langword="false"/>.</returns>
        bool TryGetByPredicate(Func<TConcept, bool> predicate, out TConcept? item, out string? reason);

        /// <summary>
        /// Lists all items currently known by the registry. Implementations may return a snapshot.
        /// </summary>
        /// <returns>An enumerable of items. The enumeration may represent a snapshot and need not reflect concurrent modifications.</returns>
        IEnumerable<TConcept> ListAll();
    }
}
