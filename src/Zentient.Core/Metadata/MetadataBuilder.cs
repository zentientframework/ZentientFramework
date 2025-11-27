// <copyright file="MetadataBuilder.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Zentient.Facades;
    using Zentient.Validation;

    /// <summary>
    /// Mutable deterministic builder for metadata. Use in hot-path construction and then call <see cref="Build"/>.
    /// The builder uses insertion semantics and preserves deterministic ordering for small sets.
    /// </summary>
    public sealed class MetadataBuilder
    {
        private readonly Dictionary<string, object?> _staging;

        /// <summary>
        /// Initialize a new builder with a small default capacity.
        /// </summary>
        public MetadataBuilder() : this(capacity: 4) { }

        /// <summary>
        /// Initialize a new builder with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">Initial dictionary capacity.</param>
        public MetadataBuilder(int capacity) => _staging = new Dictionary<string, object?>(capacity, StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the Builder class using the specified metadata as the initial state.
        /// </summary>
        /// <param name="originalMetadata">The metadata to use for initializing the builder. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="originalMetadata"/> is null.</exception>
        public MetadataBuilder(IMetadata originalMetadata)
        {
            Guard.AgainstNull(originalMetadata, nameof(originalMetadata));
            _staging = new Dictionary<string, object?>(originalMetadata.Count, StringComparer.Ordinal);
            foreach (var kv in originalMetadata) _staging[kv.Key] = kv.Value;
        }

        /// <summary>
        /// Set a metadata entry in the builder. Keys are compared using ordinal string comparison.
        /// </summary>
        /// <param name="key">Metadata key; must not be null.</param>
        /// <param name="value">Metadata value; may be null.</param>
        /// <returns>The same builder instance for fluent usage.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
        public MetadataBuilder Set(string key, object? value)
        {
            key = Guard.AgainstNullOrWhitespace(key, nameof(key));
            _staging[key] = value;
            return this;
        }

        /// <summary>
        /// Set multiple key/value pairs from an enumerable into the builder.
        /// </summary>
        /// <param name="items">Sequence of key/value pairs to set; when null the method is a no-op.</param>
        /// <returns>The same builder instance for fluent usage.</returns>
        public MetadataBuilder SetRange(IEnumerable<KeyValuePair<string, object?>> items)
        {
            if (items is null) return this;
            foreach (var kv in items) _staging[kv.Key] = kv.Value;
            return this;
        }

        /// <summary>
        /// Remove a key from the builder if present.
        /// </summary>
        /// <param name="key">Key to remove; when null the method is a no-op.</param>
        /// <returns>The same builder instance.</returns>
        public MetadataBuilder Remove(string key)
        {
            key = Guard.Trim(key);
            _staging.Remove(key);
            return this;
        }

        /// <summary>
        /// Merges the metadata from the specified <paramref name="other"/> instance into the current builder,
        /// combining nested metadata values recursively.
        /// </summary>
        /// <remarks>If both the current and the specified metadata contain values for the same
        /// key and those values are themselves metadata objects, they are merged recursively. Otherwise, values
        /// from <paramref name="other"/> overwrite existing values for matching keys.</remarks>
        /// <param name="other">The metadata to merge into the current builder. If <paramref name="other"/> is <see langword="null"/>,
        /// no changes are made.</param>
        /// <returns>The current <see cref="MetadataBuilder"/> instance with merged metadata.</returns>
        public MetadataBuilder DeepMerge(IMetadata other)
        {
            if (other is null) return this;
            foreach (var kv in other)
            {
                if (_staging.TryGetValue(kv.Key, out var existing))
                {
                    if (existing is IMetadata em && kv.Value is IMetadata om)
                    {
                        var merged = Metadata.DeepMerge(em, om);
                        _staging[kv.Key] = merged;
                        continue;
                    }
                }
                _staging[kv.Key] = kv.Value;
            }
            return this;
        }

        /// <summary>
        /// Produce an immutable <see cref="IMetadata"/> snapshot from the builder's staged entries.
        /// For small item counts an optimized linear representation is used; larger sets are hashed.
        /// </summary>
        /// <returns>An immutable, deterministic <see cref="IMetadata"/> snapshot.</returns>
        public IMetadata Build()
        {
            int count = _staging.Count;
            if (count == 0) return Metadata.Empty;

            if (count > Metadata.LinearThreshold)
            {
                return new HashedMetadata(_staging.ToImmutableDictionary(StringComparer.Ordinal));
            }

            // Deterministic ordering: sort keys
            var arr = new KeyValuePair<string, object?>[count];
            int i = 0;

            foreach (var kv in _staging.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                arr[i++] = kv;
            }

            return new LinearMetadata(arr);
        }

        /// <summary>
        /// Create a builder seeded from an existing snapshot. This copies existing key/value pairs into a new mutable builder.
        /// </summary>
        /// <param name="snapshot">Metadata snapshot to seed from; must not be null.</param>
        /// <returns>A new builder containing the snapshot's entries.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
        public static MetadataBuilder From(IMetadata snapshot)
        {
            Guard.AgainstNull(snapshot, nameof(snapshot));

            var b = new MetadataBuilder(snapshot.Count);

            foreach (var kv in snapshot)
            {
                b._staging[kv.Key] = kv.Value;
            }

            return b;
        }
    }
}
