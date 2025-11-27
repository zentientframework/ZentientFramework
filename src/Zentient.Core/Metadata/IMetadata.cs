// <copyright file="IMetadata.Metadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a read-only, immutable collection of key/value metadata entries with functional mutation helpers.
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="IMetadata"/> are immutable snapshots intended for safe sharing across threads and
    /// for use as deterministic, low-allocation metadata carriers. Mutating operations return new snapshots rather
    /// than changing the current instance. Keys are expected to be stable strings and comparisons are performed
    /// using <see cref="StringComparer.Ordinal"/> semantics by convention.
    ///
    /// Implementations SHOULD ensure that enumeration order is deterministic (for example by sorting keys) so that
    /// serializations and diffs are predictable. Use <see cref="Zentient.Metadata.Metadata.NewBuilder"/> or the
    /// provided factory helpers to construct and compose metadata snapshots.
    /// </remarks>
    public interface IMetadata : IReadOnlyDictionary<string, object?>
    {
        /// <summary>
        /// Return a new metadata snapshot with <paramref name="key"/> set to <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The metadata key to set. Must not be <see langword="null"/>.</param>
        /// <param name="value">The value to associate with <paramref name="key"/>. May be <see langword="null"/>.</param>
        /// <returns>
        /// A new <see cref="IMetadata"/> instance that contains the same entries as the current instance,
        /// except that <paramref name="key"/> maps to <paramref name="value"/>. The original instance is unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <see langword="null"/>.</exception>
        IMetadata Set(string key, object? value);

        /// <summary>
        /// Return a new metadata snapshot with the specified <paramref name="key"/> removed.
        /// </summary>
        /// <param name="key">The metadata key to remove. When <see langword="null"/> implementations may simply return the current instance.</param>
        /// <returns>
        /// A new <see cref="IMetadata"/> instance without the specified key. If the key was not present, the method
        /// may return the original snapshot instance (no-op semantics).
        /// </returns>
        IMetadata Remove(string key);

        /// <summary>
        /// Return a new metadata snapshot with the supplied entries applied on top of the current snapshot.
        /// </summary>
        /// <param name="items">Sequence of key/value pairs to set; when an incoming key already exists it overrides
        /// the current value.</param>
        /// <returns>
        /// A new <see cref="IMetadata"/> instance representing the merged result. The original instance is unchanged.
        /// </returns>
        IMetadata SetRange(IEnumerable<KeyValuePair<string, object?>> items);

        /// <summary>
        /// Attempt to retrieve the value associated with <paramref name="key"/> and convert it to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target type to attempt conversion to.</typeparam>
        /// <param name="key">Metadata key to lookup.</param>
        /// <param name="value">
        /// When the method returns, contains the converted value if the key was found and conversion succeeded;
        /// otherwise the default value of <typeparamref name="T"/>. The <paramref name="value"/> parameter is annotated
        /// with <see cref="MaybeNullWhenAttribute"/> to reflect nullability when the method returns <see langword="false"/>.
        /// </param>
        /// <returns><see langword="true"/> when the key exists and conversion to <typeparamref name="T"/> succeeded; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// Implementations should provide best-effort conversions for common primitives and enums (for example by using
        /// <see cref="Convert.ChangeType(object, Type)"/> or custom converters). Consumers should treat failure to convert as an
        /// expected outcome and handle the <see langword="false"/> return appropriately.
        /// </remarks>
        bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value);

        /// <summary>
        /// Retrieve and convert the value associated with <paramref name="key"/>, or return <paramref name="defaultValue"/>
        /// when the key is not present or conversion fails.
        /// </summary>
        /// <typeparam name="T">Target type to attempt conversion to.</typeparam>
        /// <param name="key">Metadata key to lookup.</param>
        /// <param name="defaultValue">Default value to return when the key is missing or conversion fails.</param>
        /// <returns>
        /// The converted value when the key exists and conversion succeeds; otherwise <paramref name="defaultValue"/>.
        /// </returns>
        T? GetOrDefault<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Fluent alias for <see cref="Set"/> to support expression-style updates.
        /// </summary>
        /// <param name="key">The metadata key to set.</param>
        /// <param name="value">The value to assign to the key.</param>
        /// <returns>A new <see cref="IMetadata"/> snapshot with the provided key set to the value.</returns>
        IMetadata With(string key, object? value);

        /// <summary>
        /// Creates a new builder initialized with the current metadata values.
        /// </summary>
        /// <remarks>Use the returned builder to modify metadata values and construct a new metadata
        /// instance. Changes made to the builder do not affect the original metadata object.</remarks>
        /// <returns>A <see cref="Metadata.Builder"/> instance containing the values of this metadata object.</returns>
        Metadata.Builder ToBuilder();
    }
}
