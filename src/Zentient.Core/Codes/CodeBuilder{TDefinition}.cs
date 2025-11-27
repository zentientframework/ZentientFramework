// <copyright file="CodeBuilder{TDefinition}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

// <copyright file="CodeBuilder{TDefinition}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Runtime.CompilerServices;
    using Zentient.Facades;
    using Zentient.Metadata;

    // --- Builder ------------------------------------------------------------

    /// <summary>
    /// Default lightweight builder implementation for fluently constructing immutable <see cref="ICode{TDefinition}"/> instances.
    /// </summary>
    /// <typeparam name="TDefinition">The type of the underlying code definition, which provides domain context.</typeparam>
    public sealed class CodeBuilder<TDefinition> : ICodeBuilder<TDefinition>
        where TDefinition : ICodeDefinition
    {
        private TDefinition? _definition;
        private string? _key;
        private string? _display;
        private IMetadata _metadata = Metadata.Empty;

        /// <summary>
        /// Initializes a new instance of the CodeBuilder class using the specified code definition.
        /// </summary>
        /// <param name="code">An object that provides the code definition, key, display name, and metadata to initialize the builder.
        /// Cannot be null.</param>
        internal CodeBuilder(ICode<TDefinition>? code = null)
        {
            _definition = code is null
                ? default(TDefinition)
                : code.Definition;
            _key = code?.Key;
            _display = code?.DisplayName;
            _metadata = code?.Metadata ?? Metadata.Empty;
        }

        /// <summary>
        /// Specifies the required domain-specific definition object that characterizes this code.
        /// </summary>
        /// <param name="definition">The concrete definition object.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the definition is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICodeBuilder<TDefinition> WithDefinition(TDefinition definition)
        {
            CodeValidation.ValidateDefinition(definition);
            _definition = definition;
            return this;
        }

        /// <summary>
        /// Sets the required canonical string key for the code. This key is used for caching and lookup.
        /// </summary>
        /// <param name="key">The unique string key (e.g., "HTTP_200", "PRODUCT_NOT_FOUND").</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the key is invalid based on <see cref="CodeValidation"/> rules.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICodeBuilder<TDefinition> WithKey(string key)
        {
            _key = CodeValidation.ValidateKey(key);
            return this;
        }

        /// <summary>
        /// Sets the optional human-readable display name for the code.
        /// </summary>
        /// <param name="displayName">The display name (e.g., "OK", "Resource Missing").</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the display name is invalid based on <see cref="CodeValidation"/> rules.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICodeBuilder<TDefinition> WithDisplayName(string displayName)
        {
            _display = CodeValidation.ValidateDisplay(displayName);
            return this;
        }

        /// <summary>
        /// Adds or updates a single key/value pair to the code's diagnostic metadata.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value (can be null).</param>
        /// <returns>The builder instance for chaining.</returns>
        public ICodeBuilder<TDefinition> WithMetadata(string key, object? value)
        {
            key = CodeValidation.ValidateKey(key);

            // NOTE: This implementation re-allocates IMetadata on every call which is inefficient.
            // For production-grade performance, consider refactoring to use a mutable IMetadataBuilder internally.
            if (_metadata is IMetadata m)
            {
                m.Set(key, value);
            }
            else
            {
                var b = Metadata.NewBuilder();
                b.DeepMerge(_metadata);
                b.Set(key, value);
                _metadata = b.Build();
            }
            return this;
        }

        /// <summary>
        /// Merges an existing <see cref="IMetadata"/> object into the code's diagnostic metadata.
        /// </summary>
        /// <param name="metadata">The metadata object to merge.</param>
        /// <returns>The builder instance for chaining.</returns>
        public ICodeBuilder<TDefinition> WithMetadata(IMetadata metadata)
        {
            if (metadata is null) return this;

            if (_metadata is IMetadata mm)
            {
                Metadata.DeepMerge(mm, metadata);
            }
            else
            {
                var b = Metadata.NewBuilder();
                var mergedMetadata = Metadata.DeepMerge(metadata, _metadata);
                b.SetRange(mergedMetadata);
                _metadata = b.Build();
            }
            return this;
        }

        /// <summary>
        /// Conditionally adds a single key/value pair to the metadata, allowing for fluent construction flows.
        /// </summary>
        /// <param name="condition">If <see langword="true"/>, the metadata is added.</param>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICodeBuilder<TDefinition> WithMetadataIf(bool condition, string key, object? value)
        {
            if (condition) WithMetadata(key, value);
            return this;
        }

        /// <summary>
        /// Conditionally merges an existing <see cref="IMetadata"/> object, allowing for fluent construction flows.
        /// </summary>
        /// <param name="condition">If <see langword="true"/>, the metadata is merged.</param>
        /// <param name="metadata">The metadata object to merge.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICodeBuilder<TDefinition> WithMetadataIf(bool condition, IMetadata metadata)
        {
            if (condition) WithMetadata(metadata);
            return this;
        }

        /// <summary>
        /// Finalizes the builder and retrieves the cached, immutable <see cref="ICode{TDefinition}"/> instance.
        /// If an instance with the same key already exists, it is returned instead.
        /// </summary>
        /// <returns>The canonical <see cref="ICode{TDefinition}"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if either the key or definition is missing.</exception>
        public ICode<TDefinition> Build()
        {
            CodeValidation.ValidateDefinition(_definition);
            var key = CodeValidation.ValidateKey(_key);
            var meta = _metadata ?? Metadata.Empty;
            var display = CodeValidation.ValidateDisplay(_display);

            // Uses the central factory method, ensuring caching and definition fingerprinting.
            return Code<TDefinition>.GetOrCreate(key, _definition!, meta, display);
        }
    }
}
