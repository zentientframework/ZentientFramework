// <copyright file="LinearMetadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>


// <copyright file="LinearMetadata.cs" author="Zentient Framework Team">
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
    /// Deterministic, array-backed metadata representation optimized for small collections.
    /// Keeps entries sorted for deterministic enumeration and uses linear scan lookups.
    /// </summary>
    [DebuggerDisplay("Count = {Count} (Linear)")]
    internal sealed class LinearMetadata : MetadataBase
    {
        private readonly KeyValuePair<string, object?>[] _items;

        public LinearMetadata(KeyValuePair<string, object?>[] items)
        {
            _items = Guard.AgainstNull(items, nameof(items));
        }

        /// <inheritdoc />
        public override int Count => _items.Length;

        /// <inheritdoc />
        public override IEnumerable<string> Keys => _items.Select(x => x.Key);

        /// <inheritdoc />
        public override IEnumerable<object?> Values => _items.Select(x => x.Value);

        /// <inheritdoc />
        public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)_items).GetEnumerator();

        /// <inheritdoc />
        public override bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
        {
            if (key is null)
            {
                value = null;
                return false;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                if (string.Equals(_items[i].Key, key, StringComparison.Ordinal))
                {
                    value = _items[i].Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Create a new snapshot that sets the specified key. If the key existed, replace in-place by cloning.
        /// If the addition would exceed the linear threshold, promote to a hashed representation.
        /// </summary>
        /// <param name="key">Key to set.</param>
        /// <param name="value">Value to set.</param>
        /// <returns>New metadata snapshot with the change applied.</returns>
        public override IMetadata Set(string key, object? value)
        {
            key = Guard.AgainstNullOrWhitespace(key, nameof(key));

            // Try update in-place by cloning array only when needed (fast path)
            for (int i = 0; i < _items.Length; i++)
            {
                if (string.Equals(_items[i].Key, key, StringComparison.Ordinal))
                {
                    // Replace existing entry with cloned array
                    var clone = new KeyValuePair<string, object?>[_items.Length];
                    Array.Copy(_items, clone, _items.Length);
                    clone[i] = new KeyValuePair<string, object?>(key, value);
                    return new LinearMetadata(clone);
                }
            }

            // Key not present: append or promote
            if (_items.Length + 1 <= Metadata.LinearThreshold)
            {
                var grown = new KeyValuePair<string, object?>[_items.Length + 1];
                Array.Copy(_items, grown, _items.Length);
                grown[_items.Length] = new KeyValuePair<string, object?>(key, value);
                // Ensure deterministic order by sorting the small array
                Array.Sort(grown, (a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                return new LinearMetadata(grown);
            }

            // Promote to HashedMetadata when crossing threshold
            var builder = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.Ordinal);
            for (int i = 0; i < _items.Length; i++) builder[_items[i].Key] = _items[i].Value;
            builder[key] = value;
            return new HashedMetadata(builder.ToImmutable());
        }

        /// <summary>
        /// Remove the specified key from the linear snapshot and return a new snapshot.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>New snapshot with the key removed.</returns>
        public override IMetadata Remove(string key)
        {
            Guard.AgainstNull(key, nameof(key));
            key = Guard.Trim(key);

            int idx = -1;

            for (int i = 0; i < _items.Length; i++)
            {
                if (string.Equals(_items[i].Key, key, StringComparison.Ordinal)) { idx = i; break; }
            }

            if (idx == -1)
            {
                return this;
            }

            if (_items.Length == 1)
            {
                return Metadata.Empty;
            }

            var shrunk = new KeyValuePair<string, object?>[_items.Length - 1];
            if (idx > 0)
            {
                Array.Copy(_items, 0, shrunk, 0, idx);
            }

            if (idx < _items.Length - 1)
            {
                Array.Copy(_items, idx + 1, shrunk, idx, _items.Length - idx - 1);
            }

            return new LinearMetadata(shrunk);
        }

        /// <inheritdoc/>
        public override IMetadata With(string key, object? value) => Set(key, value);

        /// <inheritdoc/>
        public override MetadataBuilder ToBuilder() => new MetadataBuilder(this);
    }
}
