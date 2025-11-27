// <copyright file="HashedMetadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Zentient.Facades;
    using Zentient.Validation;

    /// <summary>
    /// Hash-based metadata representation optimized for larger numbers of keys.
    /// Provides O(1) lookups and deterministic downsizing back to linear when small.
    /// </summary>
    [DebuggerDisplay("Count = {Count} (Hashed)")]
    internal sealed class HashedMetadata : MetadataBase
    {
        private readonly ImmutableDictionary<string, object?> _store;

        /// <summary>
        /// Initializes a new instance of the HashedMetadata class using the specified metadata store.
        /// </summary>
        /// <param name="store">An immutable dictionary containing key-value pairs to be used as the metadata store. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="store"/> is null.</exception>
        public HashedMetadata(ImmutableDictionary<string, object?> store)
        {
            _store = Guard.AgainstNull(store, nameof(store));
        }

        /// <inheritdoc />
        public override int Count => _store.Count;

        /// <inheritdoc />
        public override IEnumerable<string> Keys => _store.Keys;

        /// <inheritdoc />
        public override IEnumerable<object?> Values => _store.Values;

        /// <inheritdoc />
        public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            => _store.GetEnumerator();

        /// <inheritdoc />
        public override bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
            => _store.TryGetValue(key, out value);

        /// <inheritdoc />
        public override IMetadata Set(string key, object? value)
        {
            key = Guard.AgainstNullOrWhitespace(key, nameof(key));
            var n = _store.SetItem(key, value);
            return new HashedMetadata(n);
        }

        /// <inheritdoc />
        public override IMetadata Remove(string key)
        {
            if (key is null)
            {
                return this;
            }

            var n = _store.Remove(key);

            if (n.IsEmpty)
            {
                return Metadata.Empty;
            }

            if (n.Count <= Metadata.LinearThreshold)
            {
                // Downsize to LinearMetadata (deterministic order)
                var arr = n.OrderBy(k => k.Key, StringComparer.Ordinal).ToArray();
                var kvarr = new KeyValuePair<string, object?>[arr.Length];

                for (int i = 0; i < arr.Length; i++)
                {
                    kvarr[i] = arr[i];
                }

                return new LinearMetadata(kvarr);
            }

            return new HashedMetadata(n);
        }

        /// <inheritdoc/>
        public override IMetadata With(string key, object? value) => Set(key, value);

        /// <inheritdoc/>
        public override MetadataBuilder ToBuilder()
        {
            return new MetadataBuilder(this);
        }
    }
}
