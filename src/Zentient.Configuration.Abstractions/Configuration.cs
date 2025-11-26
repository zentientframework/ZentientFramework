// <copyright file="Configuration.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// </copyright>

namespace Zentient.Configuration
{
    using Zentient.Core;

    /// <summary>
    /// Represents a source of configuration values within the Zentient ecosystem.
    /// A configuration source declares how settings are bound and the context scopes in which
    /// it is applicable (for example environment, tenant, session).
    /// </summary>
    public interface IConfigurationSource : IConcept
    {
        /// <summary>
        /// Gets the binding semantics that describe whether context is intrinsic or extrinsic for this source.
        /// </summary>
        ContextBinding Binding { get; }

        /// <summary>
        /// Gets the set of scope lattice levels where this source is valid or should be consulted.
        /// The array enumerates one or more <see cref="ScopeLattice"/> values (for example Tenant or Environment).
        /// </summary>
        ScopeLattice[] Scopes { get; } // Changed ScopeKind -> ScopeLattice for consistency
    }

    /// <summary>
    /// Represents a single configuration setting (a key/value pair) together with its origin.
    /// </summary>
    public interface ISetting : IConcept
    {
        /// <summary>
        /// Gets the hierarchical key path for the setting (for example "app.logging.level" or "tenant:1234:featureX.enabled").
        /// Keys SHOULD be stable and follow the repository's key naming conventions.
        /// </summary>
        string KeyPath { get; }

        /// <summary>
        /// Gets the typed value of the setting. Value may be <see langword="null"/> when the setting is explicitly null or absent.
        /// Consumers are expected to perform appropriate casts or conversions for typed usage.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Gets the configuration source that produced or owns this setting.
        /// This provides provenance information useful for diagnostics and overrides.
        /// </summary>
        IConfigurationSource Source { get; }
    }

    /// <summary>
    /// Marker interface for configuration artifacts that represent persisted or serializable configuration entries.
    /// Implementations may be used for import/export, storage, or UI rendering of key/value configuration items.
    /// </summary>
    public interface IConfigurationArtifact : IConcept
    {
        /// <summary>
        /// Gets the canonical artifact key.
        /// This is typically a short stable identifier and not a hierarchical key path.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the string representation of the artifact's value.
        /// Implementations SHOULD use a stable text encoding for portability.
        /// </summary>
        string Value { get; }
    }
}