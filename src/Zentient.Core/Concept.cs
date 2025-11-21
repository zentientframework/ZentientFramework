// <copyright file="Concept.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;

    /// <summary>
    /// Facade factory helpers for creating concept instances.
    /// </summary>
    public static class Concept
    {
        /// <summary>
        /// Creates a new <see cref="IConcept"/> instance.
        /// </summary>
        /// <param name="id">Stable identifier for the concept. Must not be null or whitespace.</param>
        /// <param name="name">Human-readable name for the concept. Must not be null or whitespace.</param>
        /// <param name="description">Optional description for the concept.</param>
        /// <returns>A new <see cref="IConcept"/> instance.</returns>
        public static IConcept Create(string id, string name, string? description = null)
            => new InternalConcept(id, name, description, validate: true);

        /// <summary>
        /// Creates a new <see cref="IIdentifiable"/> instance which also carries a <see cref="Guid"/> identity.
        /// </summary>
        /// <param name="guidId">The <see cref="Guid"/> identifier. Must not be <see cref="Guid.Empty"/>.</param>
        /// <param name="id">Stable identifier for the concept. Must not be <see langword="null"/> or whitespace.</param>
        /// <param name="name">Human-readable name for the concept. Must not be <see langword="null"/> or whitespace.</param>
        /// <param name="description">Optional description for the concept.</param>
        /// <returns>A new <see cref="IIdentifiable"/> instance.</returns>
        public static IIdentifiable CreateIdentifiable(Guid guidId, string id, string name, string? description = null)
            => new InternalIdentifiable(guidId, id, name, description);

        /// <summary>
        /// Internal implementation of <see cref="IConcept"/>.
        /// </summary>
        /// <param name="Id">The stable identifier.</param>
        /// <param name="Name">The human-readable name.</param>
        /// <param name="Description">The optional description.</param>
        internal record InternalConcept(string Id, string Name, string? Description = null) : IConcept
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InternalConcept"/> class.
            /// </summary>
            /// <remarks>
            /// This constructor is primarily for use by the serialization infrastructure.
            /// </remarks>
            public InternalConcept() : this(string.Empty, string.Empty, null) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="InternalConcept"/> class with optional validation.
            /// </summary>
            /// <param name="id">The stable identifier.</param>
            /// <param name="name">The human-readable name.</param>
            /// <param name="description">The optional description.</param>
            /// <param name="validate">If <see langword="true"/>, performs validation on <paramref name="id"/> and 
            /// <paramref name="name"/>.</param>
            internal InternalConcept(string id, string name, string? description, bool validate)
                : this(id, name, description)
            {
                if (validate)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
                    ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

                    if (description is not null && description.Length == 0)
                    {
                        throw new ArgumentException("Description must be null or a non-empty string.", nameof(description));
                    }
                }
            }
        }

        /// <summary>
        /// Internal implementation of <see cref="IIdentifiable"/>.
        /// </summary>
        /// <param name="GuidId">The <see cref="Guid"/> identifier.</param>
        /// <param name="Id">The stable identifier.</param>
        /// <param name="Name">The human-readable name.</param>
        /// <param name="Description">The optional description.</param>
        /// <remarks>
        /// This type inherits from <see cref="InternalConcept"/> to reuse its implementation of <see cref="IConcept"/>.
        /// </remarks>
        internal sealed record InternalIdentifiable(Guid GuidId, string Id, string Name, string? Description = null)
                : InternalConcept(Id, Name, Description), IIdentifiable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InternalIdentifiable"/> class.
            /// </summary>
            /// <remarks>
            /// This constructor is primarily for use by the serialization infrastructure.
            /// </remarks>
            public InternalIdentifiable() : this(Guid.Empty, string.Empty, string.Empty, null) { }

            /// <summary>
            /// Initializes a new instance of the InternalIdentifiable class with the specified identifiers and optional
            /// validation.
            /// </summary>
            /// <param name="guidId">The unique GUID identifier for the object. Must not be <see cref="Guid.Empty"/> if <paramref
            /// name="validate"/> is <see langword="true"/>.</param>
            /// <param name="id">The string identifier for the object. Cannot be <see langword="null"/>, empty, or consist only of
            /// white-space characters if <paramref name="validate"/> is <see langword="true"/>.</param>
            /// <param name="name">The display name for the object. Cannot be <see langword="null"/>, empty, or consist only of white-space
            /// characters if <paramref name="validate"/> is <see langword="true"/>.</param>
            /// <param name="description">An optional description for the object, or <see langword="null"/> if no description is provided.</param>
            /// <param name="validate">A value indicating whether to validate the input parameters. If <see langword="true"/>, the constructor
            /// will validate that <paramref name="guidId"/>, <paramref name="id"/>, and <paramref name="name"/> meet
            /// their requirements.</param>
            /// <exception cref="ArgumentException">Thrown if <paramref name="validate"/> is <see langword="true"/> and <paramref name="guidId"/> is <see
            /// cref="Guid.Empty"/>, or if <paramref name="id"/> or <paramref name="name"/> is <see langword="null"/>,
            /// empty, or consists only of white-space characters.</exception>
            internal InternalIdentifiable(Guid guidId, string id, string name, string? description, bool validate)
                : this(guidId, id, name, description)
            {
                if (validate)
                {
                    if (guidId == Guid.Empty)
                    {
                        throw new ArgumentException("GUID identifier must not be empty.", nameof(guidId));
                    }

                    ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
                    ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

                    if (description is not null && description.Length == 0)
                    {
                        throw new ArgumentException("Description must be null or a non-empty string.", nameof(description));
                    }
                }
            }
        }
    }
}
