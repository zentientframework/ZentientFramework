// <copyright file="RegistryResult.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Registries
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result returned from registry registration attempts.
    /// Must be used by async try-style registration methods so callers receive rich outcome context.
    /// </summary>
    public readonly struct RegistryResult
    {
        /// <summary>
        /// Creates a successful registration result.
        /// </summary>
        public static RegistryResult Success(bool added)
            => new(added, reason: null, conflicts: Array.Empty<string>());

        /// <summary>
        /// Creates a failed registration result with reason and optional conflicts.
        /// </summary>
        /// <param name="reason">Human readable reason for the failure.</param>
        /// <param name="conflicts">Optional list of conflicting keys or other contextual conflict identifiers.</param>
        public static RegistryResult Failure(string reason, IEnumerable<string>? conflicts = null)
            => new(false, reason, conflicts ?? Array.Empty<string>());

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="added">Indicates whether the item was added.</param>
        /// <param name="reason">Human readable reason for a non-success outcome (validation/conflict). Null on success.</param>
        /// <param name="conflicts">Optional list of conflicting keys or other contextual conflict identifiers.</param>
        /// <remarks>Use static factory methods <see cref="Success(bool)"/> and <see cref="Failure(string, IEnumerable{string}?)"/> instead.</remarks>
        private RegistryResult(bool added, string? reason, IEnumerable<string> conflicts)
        {
            Added = added;
            Reason = reason;
            Conflicts = conflicts ?? Array.Empty<string>();
        }

        /// <summary>
        /// True when the item was added; false when it already existed or the operation was rejected.
        /// </summary>
        public bool Added { get; }

        /// <summary>
        /// Human readable reason for a non-success outcome (validation/conflict). Null on success.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Optional list of conflicting keys or other contextual conflict identifiers.
        /// </summary>
        public IEnumerable<string> Conflicts { get; }

        /// <summary>
        /// Returns a string that represents the current registry operation result.
        /// </summary>
        /// <returns>A string describing whether the registry operation was added successfully or failed, including the failure
        /// reason if applicable.</returns>
        public override string ToString() => Added ? "RegistryResult: Added" : $"RegistryResult: Failed - {Reason}";
    }
}
