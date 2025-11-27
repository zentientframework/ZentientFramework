// <copyright file="MetadataExtensions.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides extension methods for the IMetadata interface to facilitate conditional setting and transformation of
    /// metadata values.
    /// </summary>
    /// <remarks>These methods enable common metadata manipulation scenarios, such as setting a value only if
    /// a key is missing, replacing values for existing keys, or transforming values in place. All methods return a new
    /// IMetadata instance reflecting the requested change, preserving the original instance if no modification is made.
    /// These extensions are intended to simplify metadata management in applications that use the IMetadata
    /// abstraction.</remarks>
    public static class MetadataExtensions
    {
        /// <summary>
        /// Sets the specified key to the given value in the metadata if the key does not already exist.
        /// </summary>
        /// <param name="meta">The metadata instance in which to set the key-value pair.</param>
        /// <param name="key">The key to check for existence and potentially set in the metadata. Cannot be null.</param>
        /// <param name="value">The value to associate with the key if the key is missing. May be null.</param>
        /// <returns>The metadata instance with the key set to the specified value if it was missing; otherwise, the original
        /// metadata instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="meta"/> or <paramref name="key"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMetadata SetIfMissing(this IMetadata meta, string key, object? value)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));
            if (key is null) throw new ArgumentNullException(nameof(key));

            if (meta.ContainsKey(key)) return meta;
            return meta.Set(key, value);
        }

        /// <summary>
        /// Replaces the value associated with the specified key in the metadata only if the key already exists.
        /// </summary>
        /// <param name="meta">The metadata instance in which to replace the value.</param>
        /// <param name="key">The key whose value should be replaced. Cannot be null.</param>
        /// <param name="value">The new value to associate with the specified key. Can be null.</param>
        /// <returns>The original metadata instance if the key does not exist; otherwise, a new metadata instance with the
        /// updated value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="meta"/> or <paramref name="key"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMetadata ReplaceOnly(this IMetadata meta, string key, object? value)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (!meta.ContainsKey(key)) return meta;
            return meta.Set(key, value);
        }

        /// <summary>
        /// Returns a new metadata instance with the value associated with the specified key transformed by the provided
        /// function.
        /// </summary>
        /// <remarks>If the specified key does not exist in the metadata, <paramref name="transformer"/>
        /// is invoked with <see langword="null"/> as its argument.</remarks>
        /// <param name="meta">The metadata object to update. Cannot be null.</param>
        /// <param name="key">The key whose associated value will be transformed. Cannot be null.</param>
        /// <param name="transformer">A function that receives the current value for the specified key and returns the new value to set. Cannot be
        /// null.</param>
        /// <returns>A new metadata instance with the transformed value set for the specified key.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="meta"/>, <paramref name="key"/>, or <paramref name="transformer"/> is null.</exception>
        public static IMetadata Change(this IMetadata meta, string key, Func<object?, object?> transformer)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (transformer is null) throw new ArgumentNullException(nameof(transformer));

            meta.TryGetValue(key, out var existing);
            var changed = transformer(existing);
            return meta.Set(key, changed);
        }
    }
}
