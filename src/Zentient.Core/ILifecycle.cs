// <copyright file="ILifecycle.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// A simple, read-only lifecycle representation.
    /// </summary>
    public interface ILifecycle
    {
        /// <summary>
        /// All known lifecycle states in the order they were entered.
        /// </summary>
        IReadOnlyCollection<ILifecycleState> States { get; }

        /// <summary>
        /// Gets the current lifecycle state, if available.
        /// </summary>
        ILifecycleState? Current { get; }
    }
}
