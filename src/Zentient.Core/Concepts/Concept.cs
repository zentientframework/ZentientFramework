// <copyright file="Concept.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Concepts
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Zentient.Metadata;

    /// <summary>
    /// Public factory facade for creating concept instances.
    /// Concrete implementations are internal to allow binary compatibility improvements.
    /// </summary>
    public static class Concept
    {
        /// <summary>
        /// Creates a new concept instance with the specified key, display name, and optional description and metadata
        /// tags.
        /// </summary>
        /// <remarks>The returned concept is guaranteed to have a unique identifier. Validation is
        /// performed to ensure required parameters are provided.</remarks>
        /// <param name="key">The unique key that identifies the concept. Cannot be null or empty.</param>
        /// <param name="displayName">The display name for the concept. Cannot be null or empty.</param>
        /// <param name="description">An optional description providing additional details about the concept, or null if no description is
        /// required.</param>
        /// <param name="tags">Optional metadata tags associated with the concept. If null, an empty metadata collection is used.</param>
        /// <returns>An <see cref="IConcept"/> representing the newly created concept with the specified properties.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IConcept Create(string key, string displayName, string? description = null, IMetadata? tags = null)
            => new InternalConcept(Guid.NewGuid(), key, displayName, description, tags ?? Zentient.Metadata.Metadata.Empty, validate: true);

        /// <summary>
        /// Creates a new concept instance with a unique identifier, key, display name, and optional metadata.
        /// </summary>
        /// <param name="guidId">The globally unique identifier to assign to the concept. Must not be an empty GUID.</param>
        /// <param name="key">The key that uniquely identifies the concept within its domain. Cannot be null or empty.</param>
        /// <param name="displayName">The display name for the concept, used for presentation purposes. Cannot be null or empty.</param>
        /// <param name="description">An optional description providing additional details about the concept. May be null.</param>
        /// <param name="tags">Optional metadata tags associated with the concept. If null, an empty metadata collection is used.</param>
        /// <returns>An <see cref="IConcept"/> instance representing the newly created concept with the specified properties.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IConcept CreateIdentifiable(Guid guidId, string key, string displayName, string? description = null, IMetadata? tags = null)
            => new InternalConcept(guidId, key, displayName, description, tags ?? Zentient.Metadata.Metadata.Empty, validate: true);


        /// <summary>
        /// Represents a concept with a unique identifier, key, display name, optional description, and associated
        /// metadata tags.
        /// </summary>
        /// <param name="GuidId">The globally unique identifier that distinguishes this concept from others.</param>
        /// <param name="Key">The stable key used to reference the concept programmatically.</param>
        /// <param name="DisplayName">The human-readable name of the concept.</param>
        /// <param name="Description">An optional description providing additional details about the concept, or <see langword="null"/> if not
        /// specified.</param>
        /// <param name="Tags">The metadata tags associated with the concept, providing supplementary information.</param>
        [DebuggerDisplay("{Key} ({DisplayName})")]
        internal sealed record InternalConcept(Guid GuidId, string Key, string DisplayName, string? Description, IMetadata Tags) : IConcept
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InternalConcept"/> class.
            /// </summary>
            /// <remarks>
            /// This constructor is primarily for use by the serialization infrastructure.
            /// </remarks>
            [Obsolete("Parameterless ctor intended only for serializers.", true)]
            public InternalConcept() : this(Guid.Empty, string.Empty, string.Empty, null, Zentient.Metadata.Metadata.Empty) { }

            /// <summary>
            /// Initializes a new instance of the InternalConcept class using the specified key, display name,
            /// description, and metadata, with optional input validation.
            /// </summary>
            /// <param name="guidId">The unique GUID identifier for the concept.</param>
            /// <param name="key">The unique key that identifies the concept. Cannot be null, empty, or consist only of white-space
            /// characters if <paramref name="validate"/> is <see langword="true"/>.</param>
            /// <param name="displayName">The display name for the concept. Cannot be null, empty, or consist only of white-space characters if
            /// <paramref name="validate"/> is <see langword="true"/>.</param>
            /// <param name="description">An optional description of the concept. If provided and <paramref name="validate"/> is <see
            /// langword="true"/>, must be a non-empty string.</param>
            /// <param name="tags">The metadata associated with the concept.</param>
            /// <param name="validate">Indicates whether to validate the input parameters. If <see langword="true"/>, the constructor enforces
            /// parameter constraints.</param>
            /// <exception cref="ArgumentException">Thrown if <paramref name="validate"/> is <see langword="true"/> and <paramref name="key"/> or <paramref
            /// name="displayName"/> is null, empty, or white space, or if <paramref name="description"/> is an empty
            /// string.</exception>
            internal InternalConcept(Guid guidId, string key, string displayName, string? description, IMetadata tags, bool validate)
                : this(
                    guidId,
                    key?.Trim()!,
                    displayName?.Trim()!,
                    description?.Trim(),
                    tags)
            {
                if (!validate) return;

                // Use normalized (trimmed) variants for validation to ensure consistent behavior
                var normKey = key is null ? string.Empty : key.Trim();
                var normDisplay = displayName is null ? string.Empty : displayName.Trim();
                var normDescription = description is null ? null : description.Trim();

                if (guidId == Guid.Empty) throw new ArgumentException("GuidId must not be empty.", nameof(guidId));

                ArgumentException.ThrowIfNullOrWhiteSpace(normKey, nameof(key));
                ArgumentException.ThrowIfNullOrWhiteSpace(normDisplay, nameof(displayName));

                if (normDescription is not null && normDescription.Length == 0)
                    throw new ArgumentException("Description must be null or non-empty.", nameof(description));

                if (tags is null) throw new ArgumentNullException(nameof(tags));
            }
        }
    }
}
