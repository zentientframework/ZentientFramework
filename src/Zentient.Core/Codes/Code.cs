// <copyright file="Code.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;

    using Zentient.Metadata;

    /// <summary>
    /// Provides factory methods for creating code builders for specific code definition types.
    /// </summary>
    /// <remarks>Use the static methods of this class to instantiate code builders for types that implement
    /// <see cref="ICodeDefinition"/>. This class is intended to simplify the creation of code builder instances and
    /// ensure type safety when working with code definitions.</remarks>
    public class Code
    {
        /// <summary>Creates a new code builder for the specified definition type.</summary>
        /// <typeparam name="TDefinition">The type of code definition for which to create the builder. Must implement <see cref="ICodeDefinition"/>.</typeparam>
        /// <returns>A new instance of <see cref="ICodeBuilder{TDefinition}"/> for the specified definition type.</returns>
        public static ICodeBuilder<TDefinition> NewBuilder<TDefinition>() where TDefinition : ICodeDefinition
            => new CodeBuilder<TDefinition>();

        /// <summary>
        /// Default builder implementation for typed codes.
        /// </summary>
        /// <typeparam name="TDefinition">Definition type.</typeparam>
        public sealed class CodeBuilder<TDefinition> : ICodeBuilder<TDefinition>
            where TDefinition : ICodeDefinition
        {
            private TDefinition? _definition;
            private readonly Zentient.Metadata.Metadata.Builder _metaBuilder = Zentient.Metadata.Metadata.NewBuilder();
            private string? _key;
            private string? _display;

            /// <summary>
            /// Initializes a new instance of the CodeBuilder class with the specified key, definition, and optional
            /// metadata.
            /// </summary>
            /// <param name="key">An optional string that uniquely identifies the code builder instance. Can be null to indicate no key.</param>
            /// <param name="definition">The definition object used to configure the code builder. Can be null or omitted for default behavior.</param>
            /// <param name="metadata">Optional metadata to associate with the code builder. If provided, it is set on the internal metadata
            /// builder.</param>
            internal CodeBuilder(string? key = null, TDefinition? definition = default, IMetadata? metadata = null)
            {
                _key = key;
                _definition = definition;
                _metaBuilder.DeepMerge(metadata!);
            }

            /// <summary>
            /// Sets the unique key for the definition and returns the updated code builder instance.
            /// </summary>
            /// <param name="key">The unique key to associate with the definition. Cannot be null, empty, or consist only of white-space
            /// characters.</param>
            /// <returns>The current code builder instance with the specified key applied.</returns>
            public ICodeBuilder<TDefinition> WithKey(string key)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
                _key = key;
                return this;
            }

            /// <inheritdoc/>
            public ICodeBuilder<TDefinition> WithDefinition(TDefinition definition)
            {
                _definition = definition ?? throw new ArgumentNullException(nameof(definition));
                return this;
            }

            /// <inheritdoc/>
            public ICodeBuilder<TDefinition> WithMetadata(string key, object? value)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
                _metaBuilder.Set(key, value!);
                return this;
            }

            /// <inheritdoc/>
            public ICodeBuilder<TDefinition> WithMetadata(IMetadata metadata)
            {
                if (metadata is null) return this;
                _metaBuilder.DeepMerge(metadata);
                return this;
            }

            /// <inheritdoc/>
            public ICodeBuilder<TDefinition> WithMetadata(Action<Zentient.Metadata.Metadata.Builder> configure)
            {
                if (configure is null) return this;
                configure(_metaBuilder);
                return this;
            }

            /// <inheritdoc/>
            public ICodeBuilder<TDefinition> WithDisplayName(string displayName)
            {
                _display = displayName;
                return this;
            }

            /// <inheritdoc/>
            public ICode<TDefinition> Build()
            {
                if (_definition is null) throw new InvalidOperationException("Definition must be provided before building a Code.");
                var key = _key ?? throw new InvalidOperationException("Key must be provided before building a Code.");
                var meta = _metaBuilder.Build();
                return Code<TDefinition>.GetOrCreate(key, _definition, meta, _display);
            }
        }
    }

    /// <summary>
    /// Concrete, cached, immutable code implementation optimized for low-allocation hot-path usage.
    /// Use <see cref="Code{TDefinition}.GetOrCreate(string, TDefinition, IMetadata?, string?)"/>
    /// to obtain instances.
    /// </summary>
    /// <typeparam name="TDefinition">The code definition type.</typeparam>
    internal sealed class Code<TDefinition> : ICode<TDefinition>
        where TDefinition : ICodeDefinition
    {
        private static readonly ConcurrentDictionary<string, Code<TDefinition>> s_cache = new(StringComparer.Ordinal);

        /// <inheritdoc/>
        public string Key { get; }

        /// <inheritdoc/>
        public string? DisplayName { get; }

        /// <inheritdoc/>
        public IMetadata Metadata { get; }

        /// <inheritdoc/>
        public TDefinition Definition { get; }

        /// <summary>
        /// Internal constructor — creation must go through the factory to preserve caching semantics.
        /// </summary>
        internal Code(string key, TDefinition definition, IMetadata metadata, string? displayName)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            DisplayName = displayName;
        }

        /// <summary>
        /// Return a singleton per (TDefinition, key). Fast path is a try-get; creation uses
        /// a single concurrent add so duplicate creation is tolerated but canonicalized.
        /// </summary>
        /// <param name="key">The canonical code key.</param>
        /// <param name="definition">Typed definition instance.</param>
        /// <param name="metadata">Optional small metadata about the code.</param>
        /// <param name="displayName">Optional display name.</param>
        /// <returns>A cached <see cref="Code{TDefinition}"/> instance.</returns>
        public static Code<TDefinition> GetOrCreate(string key, TDefinition definition, IMetadata? metadata = null, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key must be non-empty.", nameof(key));
            if (definition is null) throw new ArgumentNullException(nameof(definition));

            // TODO: Consider a more sophisticated cache key and caching strategy if this grows large.

            if (s_cache.TryGetValue(key, out var existing)) return existing;

            var created = new Code<TDefinition>(key, definition, metadata ?? Zentient.Metadata.Metadata.Empty, displayName);
            return s_cache.GetOrAdd(key, created);
        }

        /// <summary>Shorthand to create from definition (kept for readability).</summary>
        public static Code<TDefinition> CreateFromDefinition(TDefinition definition, string key, IMetadata? metadata = null, string? displayName = null)
            => GetOrCreate(key, definition, metadata, displayName);

        /// <inheritdoc/>
        public override string ToString() => Key;
    }
}
