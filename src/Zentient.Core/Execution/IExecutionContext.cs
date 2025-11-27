// <copyright file="IExecutionContext.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Execution
{
    using System;
    using System.Threading;

    /// <summary>
    /// Represents a bounded execution context. Implementations produce a local <see cref="CancellationToken"/>
    /// which may be linked to a parent token. Disposing the context cancels the local token (not the parent).
    /// </summary>
    /// <summary>Execution context carrying cancellation and small ambient bag. Implementations are immutable after creation.</summary>
    public interface IExecutionContext : IDisposable
    {
        /// <summary>
        /// Cancellation token for cooperative cancellation. Parent cancellation flows into this token when linked.
        /// </summary>
        CancellationToken Cancellation { get; }

        /// <summary>
        /// Small ambient key/value bag for lightweight contextual data. Keys are convention-based strings.
        /// Throws only for unrecoverable input errors (null/whitespace key).
        /// </summary>
        /// <param name="key">The key with which the value will be retrieved. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if found; 
        /// otherwise, <see langword="null"/>.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if the key was not found; 
        /// otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
        bool TryGet(string key, out object? value, out string? reason);

        /// <summary>
        /// Typed getter for contextual values.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key with which the value will be retrieved. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if found and of the correct type;
        /// otherwise, the default value for the type.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if the key was not found or of incompatible type;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the key was found and of the correct type; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
        bool TryGet<T>(string key, out T? value, out string? reason);

        /// <summary>
        /// Store or remove a contextual value. Passing <see langword="null"/> removes the value.
        /// Throws only for unrecoverable input errors (null/whitespace key).
        /// </summary>
        /// <param name="key">The key with which the value will be associated. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="value">Value to set, or <see langword="null"/> to remove the item.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if the operation was not successful;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Implementations may impose additional constraints on the types of values stored.</remarks>
        /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
        bool TrySet(string key, object? value, out string? reason);

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="key">The key with which the value will be associated. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="value">The value to set for the specified key. May be null for reference types.</param>
        /// <param name="reason">When this method returns, contains the reason for failure if the operation was not successful;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Implementations may impose additional constraints on the types of values stored.</remarks>
        /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
        bool TrySet<T>(string key, T? value, out string? reason);

        /// <summary>
        /// Begin a minimal synchronous scope. The returned <see cref="IDisposable"/> ends the scope when disposed.
        /// Kept synchronous and intentionally lightweight to avoid allocations on hot paths.
        /// </summary>
        IDisposable BeginScope();
    }
}
