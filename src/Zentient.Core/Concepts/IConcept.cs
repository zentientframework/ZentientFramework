// <copyright file="IConcept.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zentient.Metadata;

namespace Zentient.Concepts
{
    // -----------------------------------------------------------------------
    // 5. DOMAIN KERNEL: The Canonical Concept Model
    // -----------------------------------------------------------------------

    /// <summary>
    /// The root interface for all domain concepts. Combines identity, human-readable name, 
    /// descriptive context, and immutable semantic tags (Metadata).
    /// </summary>
    public interface IConcept
    {
        /// <summary>The technical, stable, machine-readable key (replaces 'Id').</summary>
        string Key { get; }

        /// <summary>The human-readable display name (replaces 'Name').</summary>
        string DisplayName { get; }

        /// <summary>The optional, long-form human-readable description.</summary>
        string? Description { get; }

        /// <summary>A unique, globally identifiable Guid for cross-system correlation.</summary>
        Guid GuidId { get; }

        /// <summary>Immutable semantic tags for categorization and governance (Operational Metadata).</summary>
        IMetadata Tags { get; }
    }
}
