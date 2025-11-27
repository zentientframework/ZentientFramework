// <copyright file="ICode.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using Zentient.Metadata;

    /// <summary>
    /// The non-generic base interface for all structured code instances. 
    /// Provides access to the canonical key and essential metadata.
    /// </summary>
    public interface ICode
    {
        /// <summary>The canonical, unique string identifier for the code (e.g., "HTTP_200").</summary>
        string Key { get; }

        /// <summary>An optional human-readable display name for the code (e.g., "OK").</summary>
        string? DisplayName { get; }

        /// <summary>Immutable diagnostic and contextual metadata associated with the code.</summary>
        IMetadata Metadata { get; }

        /// <summary>The exact runtime type of the domain-specific definition object.</summary>
        Type DefinitionType { get; }

        /// <summary>
        /// A developer-friendly string identifier combining the definition type name and the key.
        /// Format: <c>{DefinitionType.Name}:{Key}</c>.
        /// </summary>
        string Identifier => $"{DefinitionType.FullName ?? DefinitionType.Name}:{Key}";

        /// <summary>
        /// A fully qualified, canonical identifier for the code instance, suitable for global identification.
        /// Format: <c>code:{DefinitionType.FullName}:{Key}</c>.
        /// </summary>
        string CanonicalId => $"code:{DefinitionType.FullName ?? DefinitionType.Name}:{Key}";
    }
}
