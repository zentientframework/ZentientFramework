// <copyright file="RegistryRemoveResult.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Registries
{
    /// <summary>
    /// Result returned from registry remove attempts.
    /// </summary>
    public readonly struct RegistryRemoveResult
    {
        /// <summary>
        /// Gets a value indicating whether the item has been removed.
        /// </summary>
        public bool Removed { get; }

        /// <summary>
        /// Gets the reason associated with the current state or result, if available.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryRemoveResult"/> class with the specified removal status and optional
        /// reason.
        /// </summary>
        /// <param name="removed">A value indicating whether the registry entry was successfully removed.</param>
        /// <param name="reason">An optional message describing the reason for the removal result. May be null if no additional information
        /// is available.</param>
        public RegistryRemoveResult(bool removed, string? reason = null)
        {
            Removed = removed;
            Reason = reason;
        }

        /// <summary>
        /// Returns a string that represents the current state of the registry removal result.
        /// </summary>
        /// <returns>A string indicating whether the registry entry was removed. If removed, returns "RegistryRemoveResult: Removed";
        /// otherwise, returns "RegistryRemoveResult: NotRemoved - {Reason}", where {Reason} provides the reason for the
        /// failure.</returns>
        public override string ToString() => Removed ? "RegistryRemoveResult: Removed" : $"RegistryRemoveResult: NotRemoved - {Reason}";
    }
}
