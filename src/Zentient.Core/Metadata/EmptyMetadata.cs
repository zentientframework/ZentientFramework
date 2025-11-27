// <copyright file="EmptyMetadata.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Represents an immutable, empty metadata collection.
    /// </summary>
    /// <remarks>This type provides a singleton implementation of a metadata collection with no
    /// entries. All retrieval operations return default values, and modification methods return new metadata
    /// instances or the same empty instance as appropriate. This class is typically used to represent the absence
    /// of metadata in scenarios where a non-null metadata object is required.</remarks>
    internal sealed class EmptyMetadata : MetadataBase
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
        public override MetadataBuilder ToBuilder() => new MetadataBuilder().SetRange(this);
    }
}
