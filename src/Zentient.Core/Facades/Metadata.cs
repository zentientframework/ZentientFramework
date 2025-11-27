// <copyright file="Metadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Facades
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Zentient.Metadata;

    /// <summary>
    /// Static factory and helpers for <see cref="IMetadata"/> instances.
    /// Use <see cref="Builder"/> to produce immutable snapshots with zero-cost ergonomics.
    /// This class provides efficient representations for small metadata sets (linear) and
    /// larger hashed representations. It also provides merging helpers and a conversion helper.
    /// </summary>
    public static class Metadata
    {
        private const int LinearThreshold = 8;

        /// <summary>
        /// Gets an empty metadata snapshot singleton.
        /// </summary>
        public static IMetadata Empty { get; } = new EmptyMetadata();

        /// <summary>
        /// Create a new mutable metadata builder.
        /// Use <see cref="Builder.Build"/> to obtain an immutable <see cref="IMetadata"/> snapshot.
        /// </summary>
        /// <returns>A new <see cref="Builder"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Builder NewBuilder() => new Builder();

        /// <summary>
        /// Create a single-key immutable metadata snapshot.
        /// </summary>
        /// <param name="key">Metadata key.</param>
        /// <param name="value">Metadata value.</param>
        /// <returns>An <see cref="IMetadata"/> snapshot containing the single key/value pair.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMetadata From(string key, object? value) => NewBuilder().Set(key, value).Build();

        /// <summary>
        /// Deep-merge two metadata snapshots deterministically.
        /// Keys in <paramref name="incoming"/> override keys in <paramref name="current"/>.
        /// When both values are <see cref="IMetadata"/>, the function recursively merges them.
        /// Optionally a <paramref name="conflictResolver"/> can be supplied to resolve non-metadata conflicts.
        /// </summary>
        /// <param name="current">Existing metadata snapshot. Must not be null.</param>
        /// <param name="incoming">Incoming metadata snapshot. Must not be null.</param>
        /// <param name="conflictResolver">
        /// Optional resolver called for keys that exist in both snapshots whose values are not both <see cref="IMetadata"/>.
        /// The resolver receives the key, the current value, and the incoming value and must return the chosen value.
        /// </param>
        /// <returns>A merged <see cref="IMetadata"/> snapshot.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="current"/> or <paramref name="incoming"/> is null.</exception>
        public static IMetadata DeepMerge(
                    IMetadata current,
                    IMetadata incoming,
                    Func<string, object?, object?, object?>? conflictResolver = null)
        {
            if (current is null) return incoming;
            if (incoming is null) return current;
            if (current.Count == 0) return incoming;
            if (incoming.Count == 0) return current;

            var builder = Builder.From(current);
            foreach (var kv in incoming)
            {
                var key = kv.Key;
                var incomingValue = kv.Value;

                if (current.TryGetValue(key, out var currentValue))
                {
                    if (conflictResolver != null)
                    {
                        builder.Set(key, conflictResolver(key, currentValue, incomingValue));
                        continue;
                    }

                    if (currentValue is IMetadata cMeta && incomingValue is IMetadata iMeta)
                    {
                        builder.Set(key, DeepMerge(cMeta, iMeta, null));
                        continue;
                    }

                    builder.Set(key, incomingValue);
                }
                else
                {
                    builder.Set(key, incomingValue);
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Mutable deterministic builder for metadata. Use in hot-path construction and then call <see cref="Build"/>.
        /// The builder uses insertion semantics and preserves deterministic ordering for small sets.
        /// </summary>
        public sealed class Builder
        {
            private readonly Dictionary<string, object?> _staging;

            /// <summary>
            /// Initialize a new builder with a small default capacity.
            /// </summary>
            public Builder() : this(capacity: 4) { }

            /// <summary>
            /// Initialize a new builder with the specified initial capacity.
            /// </summary>
            /// <param name="capacity">Initial dictionary capacity.</param>
            public Builder(int capacity) => _staging = new Dictionary<string, object?>(capacity, StringComparer.Ordinal);

            /// <summary>
            /// Initializes a new instance of the Builder class using the specified metadata as the initial state.
            /// </summary>
            /// <param name="originalMetadata">The metadata to use for initializing the builder. Cannot be null.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="originalMetadata"/> is null.</exception>
            public Builder(IMetadata originalMetadata)
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
            public Builder Set(string key, object? value)
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
            public Builder SetRange(IEnumerable<KeyValuePair<string, object?>> items)
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
            public Builder Remove(string key)
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
            /// <returns>The current <see cref="Builder"/> instance with merged metadata.</returns>
            public Builder DeepMerge(IMetadata other)
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
                if (count == 0) return Empty;

                if (count > LinearThreshold)
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
            public static Builder From(IMetadata snapshot)
            {
                Guard.AgainstNull(snapshot, nameof(snapshot));

                var b = new Builder(snapshot.Count);

                foreach (var kv in snapshot)
                {
                    b._staging[kv.Key] = kv.Value;
                }

                return b;
            }
        }

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
                var b = Builder.From(this);
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
            /// <returns>A <see cref="Builder"/> that can be used to modify and construct a new instance based on the current
            /// object's state.</returns>
            public abstract Builder ToBuilder();
        }

        /// <summary>
        /// Represents an immutable, empty metadata collection.
        /// </summary>
        /// <remarks>This type provides a singleton implementation of a metadata collection with no
        /// entries. All retrieval operations return default values, and modification methods return new metadata
        /// instances or the same empty instance as appropriate. This class is typically used to represent the absence
        /// of metadata in scenarios where a non-null metadata object is required.</remarks>
        private sealed class EmptyMetadata : MetadataBase
        {
            /// <inheritdoc />
            public override int Count => 0;

            /// <inheritdoc />
            public override IEnumerable<string> Keys => Enumerable.Empty<string>();

            /// <inheritdoc />
            public override IEnumerable<object?> Values => Enumerable.Empty<object?>();

            /// <inheritdoc />
            public override bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value) { value = null; return false; }

            /// <inheritdoc />
            public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, object?>>().GetEnumerator();

            /// <inheritdoc />
            public override IMetadata Set(string key, object? value)
                => new LinearMetadata(new[] { new KeyValuePair<string, object?>(key, value) });

            /// <inheritdoc />
            public override IMetadata Remove(string key) => this;

            /// <inheritdoc />
            public override IMetadata With(string key, object? value) => Set(key, value);

            /// <inheritdoc />
            public override Builder ToBuilder() => new Builder().SetRange(this);
        }

        /// <summary>
        /// Deterministic, array-backed metadata representation optimized for small collections.
        /// Keeps entries sorted for deterministic enumeration and uses linear scan lookups.
        /// </summary>
        [DebuggerDisplay("Count = {Count} (Linear)")]
        private sealed class LinearMetadata : MetadataBase
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
            /// If the addition would exceed <see cref="LinearThreshold"/>, promote to a hashed representation.
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
                if (_items.Length + 1 <= LinearThreshold)
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
                    return Empty;
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
            public override Builder ToBuilder() => new Builder(this);
        }

        /// <summary>
        /// Hash-based metadata representation optimized for larger numbers of keys.
        /// Provides O(1) lookups and deterministic downsizing back to linear when small.
        /// </summary>
        [DebuggerDisplay("Count = {Count} (Hashed)")]
        private sealed class HashedMetadata : MetadataBase
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
                    return Empty;
                }

                if (n.Count <= LinearThreshold)
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
            public override Builder ToBuilder()
            {
                return new Builder(this);
            }
        }

        /// <summary>
        /// Internal conversion helpers used by metadata getters to coerce raw values into requested types.
        /// Optimized for common primitive types and enums while providing a safe fallback for other conversions.
        /// </summary>
        internal static class TypeConverter
        {
            /// <summary>
            /// Attempt to convert a raw stored object into the requested target type <typeparamref name="T"/>.
            /// </summary>
            /// <typeparam name="T">Target conversion type.</typeparam>
            /// <param name="raw">Raw value to convert; may be null.</param>
            /// <param name="value">When conversion succeeds, contains the converted value; otherwise default.</param>
            /// <returns><see langword="true"/> if conversion succeeded; otherwise <see langword="false"/>.</returns>
            public static bool TryConvert<T>(object? raw, out T? value)
            {
                value = default;
                if (raw is null) return default(T) is null;

                // Fast direct match
                if (raw is T direct)
                {
                    value = direct;
                    return true;
                }

                var target = typeof(T);

                // Common trivial cases: string/int/long/double/bool
                if (target == typeof(string))
                {
                    if (raw is string s) { value = (T)(object)s; return true; }
                    value = (T)(object)raw.ToString()!;
                    return true;
                }

                if (target.IsPrimitive || target.IsValueType)
                {
                    try
                    {
                        if (target.IsEnum)
                        {
                            if (raw is string rs && Enum.TryParse(target, rs, true, out var ev)) { value = (T)ev!; return true; }
                            var underlying = Convert.ChangeType(raw, Enum.GetUnderlyingType(target));
                            value = (T)Enum.ToObject(target, underlying!);
                            return true;
                        }

                        var convTarget = Nullable.GetUnderlyingType(target) ?? target;
                        var converted = Convert.ChangeType(raw, convTarget);
                        value = (T)converted!;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                // Fallback: attempt system conversion
                try
                {
                    value = (T)Convert.ChangeType(raw, Nullable.GetUnderlyingType(target) ?? target);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
