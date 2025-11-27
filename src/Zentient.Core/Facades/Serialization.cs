// <copyright file="Serialization.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Facades
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Zentient.Abstractions;

    /// <summary>
    /// Serialization helpers and fallback implementations.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Returns a serializer instance that does not support serialization or deserialization operations.
        /// </summary>
        /// <remarks>Use this method when serialization functionality is intentionally unsupported or
        /// should be disabled. The returned serializer will throw exceptions if any serialization or deserialization
        /// methods are called.</remarks>
        /// <returns>An <see cref="ISerializer"/> implementation that throws <see cref="NotSupportedException"/> for all
        /// serialization and deserialization methods.</returns>
        public static ISerializer Default() => new NotSupportedSerializer();

        /// <summary>
        /// Returns a default asynchronous serializer instance that does not support serialization or deserialization
        /// operations.
        /// </summary>
        /// <remarks>This default implementation is intended for scenarios where asynchronous
        /// serialization is not supported or required. All method calls on the returned instance will result in a <see
        /// cref="NotSupportedException"/> being thrown.</remarks>
        /// <returns>An <see cref="ISerializerAsync"/> implementation that throws <see cref="NotSupportedException"/> for all
        /// serialization and deserialization methods.</returns>
        public static ISerializerAsync DefaultAsync() => new NotSupportedSerializer();

        /// <summary>
        /// Represents a serializer that does not support serialization or deserialization operations.
        /// </summary>
        /// <remarks>This type is used as a placeholder when no serializer implementation is available.
        /// All methods throw a NotSupportedException. To enable serialization, add a formatter package (such as
        /// Zentient.Formatters.Json) or provide an ISerializer implementation via your host.</remarks>
        [DebuggerNonUserCode]
        [ExcludeFromCodeCoverage]
        internal sealed class NotSupportedSerializer : ISerializer, ISerializerAsync
        {
            private const string Message = "No serializer implementation available. Add a formatter package (e.g., Zentient.Formatters.Json) or plug ISerializer via your host.";

            /// <inheritdoc/>
            [StackTraceHidden]
            public string Serialize<T>(T item)
                => throw new NotSupportedException(Message);

            /// <inheritdoc/>
            [StackTraceHidden]
            public T? Deserialize<T>(string payload)
                => throw new NotSupportedException(Message);

            /// <inheritdoc/>
            [StackTraceHidden]
            public Task<string> SerializeAsync<T>(T item, CancellationToken token = default)
                => throw new NotSupportedException(Message);

            /// <inheritdoc/>
            [StackTraceHidden]
            public Task<T?> DeserializeAsync<T>(string payload, CancellationToken token = default)
                => throw new NotSupportedException(Message);
        }
    }
}
