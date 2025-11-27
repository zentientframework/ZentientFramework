// <copyright file="IMetadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System.Collections.Generic;

    /// <summary>
    /// Optional small marker interface to indicate metadata provides deterministic ordered entries.
    /// </summary>
    /// <remarks>
    /// Implement this interface for metadata implementations that can guarantee iteration order
    /// (useful for deterministic serialization scenarios).
    /// </remarks>
    public interface IDeterministicMetadata : IMetadata
    {
        /// <summary>
        /// Gets the entries in a deterministic, well-defined order.
        /// </summary>
        IReadOnlyList<KeyValuePair<string, object?>> OrderedEntries { get; }
    }
}
