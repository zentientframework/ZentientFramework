// <copyright file="ICodeBuilder{TDefinition}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using Zentient.Metadata;

    /// <summary>
    /// Defines the contract for a fluent builder used to construct immutable <see cref="ICode{TDefinition}"/> instances.
    /// </summary>
    /// <typeparam name="TDefinition">The type of the domain-specific definition.</typeparam>
    public interface ICodeBuilder<TDefinition>
        where TDefinition : ICodeDefinition
    {
        /// <summary>Sets the required domain definition.</summary>
        ICodeBuilder<TDefinition> WithDefinition(TDefinition definition);

        /// <summary>Sets the required canonical code key.</summary>
        ICodeBuilder<TDefinition> WithKey(string key);

        /// <summary>Sets the optional human-readable display name.</summary>
        ICodeBuilder<TDefinition> WithDisplayName(string displayName);

        /// <summary>Adds or updates a single metadata key/value pair.</summary>
        ICodeBuilder<TDefinition> WithMetadata(string key, object? value);

        /// <summary>Merges an existing <see cref="IMetadata"/> object.</summary>
        ICodeBuilder<TDefinition> WithMetadata(IMetadata metadata);

        /// <summary>Conditionally adds a single metadata key/value pair.</summary>
        ICodeBuilder<TDefinition> WithMetadataIf(bool condition, string key, object? value);

        /// <summary>Conditionally merges an existing <see cref="IMetadata"/> object.</summary>
        ICodeBuilder<TDefinition> WithMetadataIf(bool condition, IMetadata metadata);

        /// <summary>Finalizes and retrieves the canonical, cached <see cref="ICode{TDefinition}"/> instance.</summary>
        ICode<TDefinition> Build();
    }
}
