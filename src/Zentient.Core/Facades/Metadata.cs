// <copyright file="Metadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Facades
{
    using System;
    using System.Runtime.CompilerServices;
    using Zentient.Metadata;

    /// <summary>
    /// Static factory and helpers for <see cref="IMetadata"/> instances.
    /// Use <see cref="MetadataBuilder"/> to produce immutable snapshots with zero-cost ergonomics.
    /// This class provides efficient representations for small metadata sets (linear) and
    /// larger hashed representations. It also provides merging helpers and a conversion helper.
    /// </summary>
    public static class Metadata
    {
        internal const int LinearThreshold = 8;

        /// <summary>
        /// Gets an empty metadata snapshot singleton.
        /// </summary>
        public static IMetadata Empty { get; } = new EmptyMetadata();

        /// <summary>
        /// Create a new mutable metadata builder.
        /// Use <see cref="MetadataBuilder.Build"/> to obtain an immutable <see cref="IMetadata"/> snapshot.
        /// </summary>
        /// <returns>A new <see cref="MetadataBuilder"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MetadataBuilder NewBuilder() => new MetadataBuilder();

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

            var builder = MetadataBuilder.From(current);
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
    }
}
