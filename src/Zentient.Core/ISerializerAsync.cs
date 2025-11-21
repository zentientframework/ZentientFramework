// <copyright file="ISerializerAsync.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Async serializer contract.
    /// </summary>
    public interface ISerializerAsync
    {
        /// <summary>
        /// Asynchronously serializes the specified item to a string representation.
        /// The implementation may throw for unrecoverable configuration errors.
        /// </summary>
        /// <typeparam name="T">The type of the item to serialize.</typeparam>
        /// <param name="item">The item to serialize.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>A string representation of the serialized item.</returns>
        /// <exception cref="NotSupportedException">Thrown when no serializer implementation is available.</exception>
        Task<string> SerializeAsync<T>(T item, CancellationToken token = default);

        /// <summary>
        /// Asynchronously deserializes the specified string payload into an instance of <typeparamref name="T"/>.
        /// Returns <c>null</c> if payload is empty and <typeparamref name="T"/> is nullable.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="payload">The string payload to deserialize.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>An instance of <typeparamref name="T"/> deserialized from the payload.</returns>
        /// <exception cref="NotSupportedException">Thrown when no serializer implementation is available.</exception>
        Task<T?> DeserializeAsync<T>(string payload, CancellationToken token = default);
    }
}
