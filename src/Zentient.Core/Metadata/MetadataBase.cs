// <copyright file="MetadataBase.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Base abstract implementation used by concrete metadata representations.
    /// Provides helpers and default implementations for common operations.
    /// </summary>
    internal abstract class MetadataBase : IMetadata
    {
        /// <inheritdoc />
        public abstract int Count { get; }

        /// <inheritdoc />
        public abstract IEnumerable<string> Keys { get; }

        /// <inheritdoc />
        public abstract IEnumerable<object?> Values { get; }

        /// <inheritdoc />
        public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value);

        /// <inheritdoc />
        public abstract IEnumerator<KeyValuePair<string, object?>> GetEnumerator();

        /// <summary>
        /// Return a new snapshot with the specified key set to the provided value.
        /// Concrete implementations must implement this efficiently.
        /// </summary>
        /// <param name="key">Key to set.</param>
        /// <param name="value">Value to assign.</param>
        /// <returns>A new <see cref="IMetadata"/> snapshot with the change applied.</returns>
        public abstract IMetadata Set(string key, object? value);

        /// <summary>
        /// Remove a key from the snapshot and return a new snapshot reflecting the removal.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>A new snapshot with the key removed (or the same instance when no change).</returns>
        public abstract IMetadata Remove(string key);

        /// <summary>
        /// Alias for <see cref="Set"/> used to support fluent semantics.
        /// </summary>
        /// <param name="key">Key to set.</param>
        /// <param name="value">Value to set.</param>
        /// <returns>A new snapshot with the set applied.</returns>
        public abstract IMetadata With(string key, object? value);

        /// <inheritdoc />
        public bool ContainsKey(string key) => TryGetValue(key, out _);

        /// <inheritdoc />
        public object? this[string key] => TryGetValue(key, out var v) ? v : throw new KeyNotFoundException(key);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Produce a new snapshot with multiple key/value pairs merged on top of the current snapshot.
        /// </summary>
        /// <param name="items">Items to set; may be null.</param>
        /// <returns>A merged <see cref="IMetadata"/> snapshot.</returns>
        public IMetadata SetRange(IEnumerable<KeyValuePair<string, object?>> items)
        {
            items = items ?? Enumerable.Empty<KeyValuePair<string, object?>>();
            var b = MetadataBuilder.From(this);
            b.SetRange(items);
            return b.Build();
        }

        /// <summary>
        /// Try to convert a stored raw object into the requested generic type <typeparamref name="T"/>.
        /// Uses optimized conversions for primitives, enums and strings and falls back to system conversions.
        /// </summary>
        /// <typeparam name="T">Target type to convert to.</typeparam>
        /// <param name="key">Metadata key.</param>
        /// <param name="value">When successful, contains the converted value; otherwise default.</param>
        /// <returns><see langword="true"/> when conversion succeeds; otherwise <see langword="false"/>.</returns>
        public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
        {
            if (TryGetValue(key, out var raw))
            {
                return TypeConverter.TryConvert(raw, out value);
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Get a typed value or the provided default when the key does not exist or conversion fails.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="key">Metadata key.</param>
        /// <param name="defaultValue">Fallback value when key is absent or conversion fails.</param>
        /// <returns>Converted value or <paramref name="defaultValue"/>.</returns>
        public T? GetOrDefault<T>(string key, T? defaultValue = default)
            => TryGet<T>(key, out var v) ? v : defaultValue;

        /// <summary>
        /// Creates a new builder instance initialized with the current object's values.
        /// </summary>
        /// <returns>A <see cref="MetadataBuilder"/> that can be used to modify and construct a new instance based on the current
        /// object's state.</returns>
        public abstract MetadataBuilder ToBuilder();
    }
}
