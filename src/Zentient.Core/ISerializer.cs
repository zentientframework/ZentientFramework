// <copyright file="ISerializer.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;

    /// <summary>
    /// Serializer contract used by higher-level packages. Implementations are free to choose the format.
    /// Absence of a serializer implementation is a configuration error (unrecoverable) and may throw.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serialize the provided item to a string payload.
        /// </summary>
        /// <typeparam name="T">The type of the item to serialize.</typeparam>
        /// <param name="item">The item to serialize.</param>
        /// <returns>A string representation of the serialized item.</returns>
        /// <exception cref="NotSupportedException">Thrown when no serializer implementation is available.</exception>
        string Serialize<T>(T item);

        /// <summary>
        /// Deserialize the provided payload into an instance of <typeparamref name="T"/>.
        /// Return <c>null</c> if payload is empty and <typeparamref name="T"/> is nullable.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="payload">The string payload to deserialize.</param>
        /// <returns>An instance of <typeparamref name="T"/> deserialized from the payload.</returns>
        /// <exception cref="NotSupportedException">Thrown when no serializer implementation is available.</exception>
        T? Deserialize<T>(string payload);
    }
}
