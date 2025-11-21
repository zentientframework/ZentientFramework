// <copyright file="IRegistryObserver.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    /// <summary>
    /// Observer contract for registries. Intended for optional telemetry/auditing integration.
    /// Implementations should avoid throwing; exceptions from observers are considered best-effort and should be handled by callers.
    /// </summary>
    /// <typeparam name="TConcept">Concept type observed by the registry.</typeparam>
    public interface IRegistryObserver<in TConcept>
        where TConcept : IConcept
    {
        /// <summary>
        /// Handles logic to be executed when an item is registered. Cannot be null.
        /// </summary>
        /// <param name="item">The item that was registered.</param>
        void OnRegistered(TConcept item);

        /// <summary>
        /// Handles logic to be performed when an item with the specified identifier is removed.
        /// </summary>
        /// <param name="id">The identifier of the item that was removed.</param>
        void OnRemoved(string id);
    }
}
