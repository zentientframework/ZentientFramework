// <copyright file="IConcept.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;

    /// <summary>
    /// Minimal conceptual atom: identity, display name and optional description.
    /// Implementations are expected to be effectively immutable and thread-safe.
    /// </summary>
    public interface IConcept
    {
        /// <summary>
        /// Stable identifier for the concept. This is an opaque string and may be a <see cref="Guid"/>,
        /// a structured identifier, or any other stable identifier chosen by implementations.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Short human-readable name intended for display or logging. Not intended to be
        /// used as a canonical identifier.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optional description providing additional context; may be <c>null</c> when not provided.
        /// </summary>
        string? Description { get; }
    }
}
