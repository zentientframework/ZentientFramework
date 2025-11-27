// <copyright file="IContextExtension.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Execution
{
    using System;

    /// <summary>
    /// Marker for optional context-level extensions implemented by higher-level packages.
    /// </summary>
    public interface IContextExtension
    {
        /// <summary>
        /// Gets the name associated with the current instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version information for the current instance.
        /// </summary>
        Version Version { get; }
    }
}
