// <copyright file="Execution.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    /// Execution context helpers for creating and working with execution scopes.
    /// </summary>
    public static class Execution
    {
        /// <summary>Create a new execution context. Parent token is optional; parent cancellations will flow into child.</summary>
        /// <param name="parent">Optional parent cancellation token.</param>
        /// <returns>A new <see cref="IExecutionContext"/> instance.</returns>
        public static IExecutionContext Create(CancellationToken parent = default)
            => new ExecutionContextImpl(parent);

        /// <summary>
        /// Internal implementation of <see cref="IExecutionContext"/>.
        /// </summary>
        internal sealed class ExecutionContextImpl : IExecutionContext
        {
            private readonly ConcurrentDictionary<string, object?> _bag = new();
            private readonly CancellationTokenSource _localCts = new();
            private readonly CancellationTokenSource? _linkedCts;
            private bool _disposed;
            private readonly object _globalCreateLock = new();

            /// <summary>
            /// Initializes a new instance of the <see cref="ExecutionContextImpl"/> class.
            /// </summary>
            /// <param name="parent">Optional parent cancellation token.</param>
            public ExecutionContextImpl(CancellationToken parent = default)
            {
                if (parent != default)
                {
                    _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parent, _localCts.Token);
                    Cancellation = _linkedCts.Token;
                }
                else
                {
                    Cancellation = _localCts.Token;
                }
            }

            /// <inheritdoc/>
            public CancellationToken Cancellation { get; }

            /// <inheritdoc/>
            public bool TryGet(string key, out object? value, out string? reason)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
                Cancellation.ThrowIfCancellationRequested();

                var result = _bag.TryGetValue(key, out value);
                reason = result ? null : $"Key '{key}' not found in execution context.";
                return result;
            }

            /// <inheritdoc/>
            public bool TryGet<T>(string key, out T? value, out string? reason)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
                Cancellation.ThrowIfCancellationRequested();

                if (_bag.TryGetValue(key, out var v) && v is T t)
                {
                    value = t;
                    reason = null;
                    return true;
                }

                value = default;
                reason = $"Key '{key}' not found in execution context or value is of incompatible type.";
                return false;
            }

            /// <inheritdoc/>
            public bool TrySet(string key, object? value, out string? reason)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
                Cancellation.ThrowIfCancellationRequested();

                if (value is null)
                {
                    var removed = _bag.TryRemove(key, out _);
                    if (removed)
                    {
                        reason = null;
                        return true;
                    }

                    reason = $"Key '{key}' not found in execution context.";
                    return false;
                }
                else
                {
                    var added = _bag.TryAdd(key, value);
                    if (added)
                    {
                        reason = null;
                        return true;
                    }

                    reason = $"Key '{key}' already exists in execution context.";
                    return false;
                }
            }

            /// <inheritdoc/>
            public bool TrySet<T>(string key, T? value, out string? reason)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

                if (value is null)
                {
                    var removed = _bag.TryRemove(key, out _);
                    if (removed)
                    {
                        reason = null;
                        return true;
                    }

                    reason = $"Key '{key}' not found in execution context.";
                    return false;
                }

                var added = _bag.TryAdd(key, value);
                if (added)
                {
                    reason = null;
                    return true;
                }
                else
                {
                    reason = $"Key '{key}' already exists in execution context.";
                    return false;
                }
            }

            public IDisposable BeginScope() => ScopeToken.Instance;

            public void Dispose()
            {
                if (_disposed) return;

                lock (_globalCreateLock)
                {
                    if (_disposed) return;

                    try
                    {
                        try { _localCts.Cancel(); } catch { /* best-effort */ }
                        _localCts.Dispose();
                    }
                    finally
                    {
                        _linkedCts?.Dispose();
                        _disposed = true;
                    }
                }
            }

            private sealed class ScopeToken : IDisposable
            {
                public static readonly ScopeToken Instance = new();
                private ScopeToken() { }
                public void Dispose() { /* no-op */ }
            }
        }
    }
}
