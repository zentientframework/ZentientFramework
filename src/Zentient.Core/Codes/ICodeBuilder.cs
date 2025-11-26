// <copyright file="ICodeBuilder.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;

    using Zentient.Metadata;

    /// <summary>
    /// Builder interface for <see cref="ICode{TDefinition}"/>.
    /// </summary>
    /// <typeparam name="TDefinition">The code definition type.</typeparam>
    public interface ICodeBuilder<TDefinition>
        where TDefinition : ICodeDefinition
    {
        /// <summary>
        /// Specifies the definition to be used by the code builder.
        /// </summary>
        /// <param name="definition">The definition object that configures the code builder. Cannot be null.</param>
        /// <returns>An instance of <see cref="ICodeBuilder{TDefinition}"/> configured with the provided definition.</returns>
        ICodeBuilder<TDefinition> WithDefinition(TDefinition definition);

        /// <summary>
        /// Adds a metadata entry to the current definition using the specified key and value.
        /// </summary>
        /// <remarks>If a metadata entry with the same key already exists, it may be overwritten. This
        /// method enables fluent chaining for building definitions with multiple metadata entries.</remarks>
        /// <param name="key">The metadata key to associate with the value. Cannot be null or empty.</param>
        /// <param name="value">The metadata value to associate with the specified key. May be null.</param>
        /// <returns>An instance of <see cref="ICodeBuilder{TDefinition}"/> with the specified metadata entry added.</returns>
        ICodeBuilder<TDefinition> WithMetadata(string key, object? value);

        /// <summary>
        /// Attaches the specified metadata to the code definition being built.
        /// </summary>
        /// <param name="metadata">The metadata to associate with the code definition. Cannot be null.</param>
        /// <returns>An instance of <see cref="ICodeBuilder{TDefinition}"/> that includes the provided metadata.</returns>
        ICodeBuilder<TDefinition> WithMetadata(IMetadata metadata);

        /// <summary>
        /// Specifies a unique key for the definition being built.
        /// </summary>
        /// <param name="key">The key that uniquely identifies the definition. Cannot be null or empty.</param>
        /// <returns>An instance of <see cref="ICodeBuilder{TDefinition}"/> configured with the specified key.</returns>
        ICodeBuilder<TDefinition> WithKey(string key);

        /// <summary>
        /// Sets the display name for the definition being built.
        /// </summary>
        /// <param name="v">The display name to assign to the definition. Cannot be null or empty.</param>
        /// <returns>An <see cref="ICodeBuilder{TDefinition}"/> instance that can be used to continue building the definition
        /// with the specified display name.</returns>
        ICodeBuilder<TDefinition> WithDisplayName(string v);

        /// <summary>
        /// Builds and returns a code representation based on the current definition and configuration.
        /// </summary>
        /// <remarks>Subsequent modifications to the builder do not affect the returned code instance.
        /// This method can be called multiple times to generate distinct code representations from different
        /// configurations.</remarks>
        /// <returns>An <see cref="ICode{TDefinition}"/> instance representing the constructed code. The returned object reflects
        /// the current state of the builder and its settings.</returns>
        ICode<TDefinition> Build();

        /// <summary>
        /// Configures metadata for the current definition using the specified builder action.
        /// </summary>
        /// <remarks>Use this method to attach custom metadata to the definition. The provided action is
        /// invoked with a builder that allows setting key-value pairs or other metadata attributes. This method
        /// supports fluent chaining.</remarks>
        /// <param name="configure">An action that receives a <see cref="Metadata.Builder"/> to configure metadata properties for the
        /// definition.</param>
        /// <returns>An <see cref="ICodeBuilder{TDefinition}"/> instance that reflects the updated metadata configuration.</returns>
        ICodeBuilder<TDefinition> WithMetadata(Action<Metadata.Builder> configure);
    }
}
