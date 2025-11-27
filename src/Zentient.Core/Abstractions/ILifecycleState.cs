// <copyright file="ILifecycleState.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Abstractions
{
    using System;

    /// <summary>
    /// Represents a named lifecycle state with optional timestamp when entered.
    /// </summary>
    public interface ILifecycleState
    {
        /// <summary>
        /// Name label of the lifecycle state.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the date and time when the entry was created, or null if the entry time is not set.
        /// </summary>
        DateTimeOffset? EnteredAt { get; }
    }
}
