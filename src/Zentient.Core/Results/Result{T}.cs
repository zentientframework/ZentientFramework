// <copyright file="Result.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis; // For NotNullWhen
using System.Linq;
using System.Threading.Tasks;
using Zentient.Errors;
using Zentient.Metadata;

namespace Zentient.Results
{
    /// <summary>
    /// High-performance discriminated result type carrying either a success value of type <typeparamref name="T"/>
    /// or an <see cref="Error"/>. This struct is immutable, ensures minimal allocations on the success path,
    /// and simplifies functional programming patterns like Map and Bind.
    /// </summary>
    /// <typeparam name="T">Type of the successful payload.</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct Result<T>
    {
        private readonly T? _value;
        private readonly Error? _error;
        private readonly IMetadata _operationalMetadata;
        private readonly ImmutableArray<Error> _warnings;

        // ----------------------
        // Constructors
        // ----------------------

        /// <summary>Internal constructor for success results.</summary>
        internal Result(T value, IMetadata? operationalMetadata, ImmutableArray<Error> warnings)
        {
            _error = null;
            _value = value;
            _operationalMetadata = operationalMetadata ?? Metadata.Metadata.Empty;
            _warnings = NormalizeWarnings(warnings);
        }

        /// <summary>Internal constructor for failure results.</summary>
        internal Result(Error error, IMetadata? operationalMetadata, ImmutableArray<Error> warnings)
        {
            if (error is null) throw new ArgumentNullException(nameof(error));
            _error = error;
            _value = default;
            _operationalMetadata = operationalMetadata ?? Metadata.Metadata.Empty;
            _warnings = NormalizeWarnings(warnings);
        }

        // ----------------------
        // Fast-path factories
        // ----------------------

        /// <summary>Creates a successful result with the given value, no metadata, and no warnings.</summary>
        public static Result<T> Ok(T value) => new(value, Metadata.Metadata.Empty, ImmutableArray<Error>.Empty);

        /// <summary>Creates a successful result with the given value and existing operational metadata.</summary>
        public static Result<T> Ok(T value, IMetadata operationalMetadata) =>
            new(value, operationalMetadata, ImmutableArray<Error>.Empty);

        /// <summary>
        /// Creates a successful result, configuring operational metadata via a builder action.
        /// </summary>
        public static Result<T> Ok(T value, Action<Metadata.Metadata.Builder> configureOperationalMetadata)
        {
            if (configureOperationalMetadata is null) return Ok(value);

            var mb = Metadata.Metadata.NewBuilder();
            configureOperationalMetadata(mb);

            return new Result<T>(value, mb.Build(), ImmutableArray<Error>.Empty);
        }

        /// <summary>Creates a failed result with the given error, no metadata, and no warnings.</summary>
        public static Result<T> Fail(Error error) => new(error, Metadata.Metadata.Empty, ImmutableArray<Error>.Empty);

        /// <summary>Creates a failed result with the given error and existing operational metadata.</summary>
        public static Result<T> Fail(Error error, IMetadata operationalMetadata) =>
            new(error, operationalMetadata, ImmutableArray<Error>.Empty);

        /// <summary>
        /// Creates a failed result by fluently configuring an error and optionally operational metadata.
        /// </summary>
        public static Result<T> Fail(Action<Error.Builder> configureError, Action<Metadata.Metadata.Builder>? configureOperationalMetadata = null)
        {
            var eb = Error.NewBuilder("UNKNOWN_OPERATION_FAILURE", "An operation failed to produce a structured error.");
            configureError?.Invoke(eb);
            var err = eb.Build();

            IMetadata? meta = null;
            if (configureOperationalMetadata is not null)
            {
                var mb = Metadata.Metadata.NewBuilder();
                configureOperationalMetadata(mb);
                meta = mb.Build();
            }

            return new Result<T>(error: err, operationalMetadata: meta, warnings: ImmutableArray<Error>.Empty);
        }

        // ----------------------
        // Properties
        // ----------------------

        /// <summary>True when the result represents success.</summary>
        [MemberNotNullWhen(true, nameof(Value))]
        [MemberNotNullWhen(false, nameof(Error))]
        public bool IsSuccess => _error is null;

        /// <summary>True when the result represents failure.</summary>
        [MemberNotNullWhen(true, nameof(Error))]
        [MemberNotNullWhen(false, nameof(Value))]
        public bool IsFailure => _error is not null;

        /// <summary>Returns the success value or throws <see cref="InvalidOperationException"/> when result is failure.</summary>
        public T Value
        {
            get
            {
                if (IsFailure) throw new InvalidOperationException($"Result failed: {_error?.Message}");
                // Suppress warning: IsSuccess check ensures _value is not null when T is notnull
                return _value!;
            }
        }

        /// <summary>Returns the error for failure results or throws <see cref="InvalidOperationException"/> when result is success.</summary>
        public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Result succeeded.");

        /// <summary>
        /// Immutable snapshot of operational metadata associated with this result instance. 
        /// Guaranteed to be non-null (returns <see cref="Metadata"/>.Empty if none is present).
        /// </summary>
        public IMetadata OperationalMetadata => _operationalMetadata;

        /// <summary>
        /// A normalized, immutable array of non-fatal errors or warnings produced during the operation.
        /// Guaranteed to be non-default (<see cref="ImmutableArray{T}.IsDefault"/> is false).
        /// </summary>
        public ImmutableArray<Error> Warnings => _warnings.IsDefault ? ImmutableArray<Error>.Empty : _warnings;

        // ----------------------
        // Mutators & Monads
        // ----------------------

        /// <summary>Returns a copy of this result with new operational metadata, performing a no-op if the metadata is identical.</summary>
        public Result<T> WithOperationalMetadata(IMetadata metadata)
        {
            var newMeta = metadata ?? Metadata.Metadata.Empty;
            if (ReferenceEquals(_operationalMetadata, newMeta)) return this;

            return IsSuccess
                ? new Result<T>(_value!, newMeta, Warnings)
                : new Result<T>(_error!, newMeta, Warnings);
        }

        /// <summary>Configures operational metadata using a builder and returns a new <see cref="Result{T}"/> instance.</summary>
        public Result<T> WithOperationalMetadata(Action<Metadata.Metadata.Builder> configure)
        {
            if (configure is null) return this;

            var mb = Metadata.Metadata.NewBuilder();
            // Start from existing metadata to allow mutation/merging
            mb.SetRange(OperationalMetadata);

            configure(mb);
            var built = mb.Build();

            return WithOperationalMetadata(built);
        }

        /// <summary>Returns a copy with the given warnings merged and normalized. Avoids allocation when identical.</summary>
        public Result<T> WithWarnings(IEnumerable<Error> warnings)
        {
            if (warnings is null) return this;

            var add = ImmutableArray.CreateRange(warnings);
            if (add.IsEmpty && _warnings.IsEmpty) return this;

            var merged = _warnings.IsEmpty ? add : _warnings.AddRange(add);
            var norm = NormalizeWarnings(merged);

            // Check if the resulting array is identical to avoid creating a new Result struct
            if (norm.SequenceEqual(_warnings)) return this;

            return IsSuccess
                ? new Result<T>(_value!, OperationalMetadata, norm)
                : new Result<T>(_error!, OperationalMetadata, norm);
        }

        /// <summary>
        /// Monadic map operation: transforms the successful value into another type while preserving metadata and warnings. 
        /// Failure preserves the original error, warnings, and metadata.
        /// </summary>
        public Result<U> Map<U>(Func<T, U> map)
        {
            if (map is null) throw new ArgumentNullException(nameof(map));

            if (IsFailure)
            {
                // Preserve error, metadata, and warnings
                return new Result<U>(_error!, OperationalMetadata, Warnings);
            }

            var mapped = map(_value!);
            // Preserve metadata and warnings
            return new Result<U>(mapped, OperationalMetadata, Warnings);
        }

        /// <summary>
        /// Monadic bind operation: chains to a result-returning function.
        /// If successful, the operation proceeds. Operational metadata and warnings are merged from both results.
        /// </summary>
        public Result<U> Bind<U>(Func<T, Result<U>> binder)
        {
            if (binder is null) throw new ArgumentNullException(nameof(binder));

            if (IsFailure)
            {
                // If failure, propagate the original error, metadata, and warnings.
                return new Result<U>(_error!, OperationalMetadata, Warnings);
            }

            var nextResult = binder(_value!);

            // 1. Merge Warnings: Original warnings + new result's warnings.
            var mergedWarnings = Warnings.AddRange(nextResult.Warnings);

            // 2. Merge Metadata: Deep merge original metadata into the new result's metadata.
            // This favors the metadata produced by the later/inner operation (nextResult).
            var mergedMetaBuilder = Metadata.Metadata.NewBuilder();
            mergedMetaBuilder.SetRange(nextResult.OperationalMetadata);
            // Deep merge original data, allowing the inner metadata to win on conflicts.
            mergedMetaBuilder.DeepMerge(OperationalMetadata);

            var finalMeta = mergedMetaBuilder.Build();

            // Create final result, preserving the success/failure state of the nextResult.
            return nextResult.IsSuccess
                ? new Result<U>(nextResult._value!, finalMeta, mergedWarnings)
                : new Result<U>(nextResult._error!, finalMeta, mergedWarnings);
        }

        /// <summary>Async bind optimized to avoid state machine allocation on the synchronous success path.</summary>
        public ValueTask<Result<U>> BindAsync<U>(Func<T, ValueTask<Result<U>>> binder)
        {
            if (binder is null) throw new ArgumentNullException(nameof(binder));

            if (IsFailure)
            {
                // Propagate failure synchronously
                return new ValueTask<Result<U>>(new Result<U>(_error!, OperationalMetadata, Warnings));
            }

            // Invoke the async binder
            var task = binder(_value!);

            // Capture instance members to avoid capturing 'this' inside the local function / continuation.
            var outerWarnings = Warnings;
            var outerOperationalMetadata = OperationalMetadata;

            // If the task completed synchronously, run the continuation synchronously to avoid allocation.
            if (task.IsCompletedSuccessfully)
            {
                var nextResult = task.Result;
                var finalResult = BindContinuation(nextResult, outerWarnings, outerOperationalMetadata);
                return new ValueTask<Result<U>>(finalResult);
            }

            // Otherwise, allocate the state machine but pass captured data into the continuation to avoid 'this' capture.
            return new ValueTask<Result<U>>(
                task.AsTask().ContinueWith(
                    t => BindContinuation(t.Result, outerWarnings, outerOperationalMetadata),
                    TaskContinuationOptions.ExecuteSynchronously
                )
            );

            static Result<U> BindContinuation(Result<U> nextResult, ImmutableArray<Error> outerWarnings, IMetadata outerOperationalMetadata)
            {
                // Merge warnings: outerWarnings + nextResult.Warnings
                var mergedWarnings = outerWarnings.AddRange(nextResult.Warnings);

                // Merge metadata: start from nextResult's metadata, deep-merge the outer metadata (outer wins resolved inside DeepMerge semantics)
                var mergedMetaBuilder = Metadata.Metadata.NewBuilder();
                mergedMetaBuilder.SetRange(nextResult.OperationalMetadata);
                mergedMetaBuilder.DeepMerge(outerOperationalMetadata);

                var finalMeta = mergedMetaBuilder.Build();

                return nextResult.IsSuccess
                    ? new Result<U>(nextResult._value!, finalMeta, mergedWarnings)
                    : new Result<U>(nextResult._error!, finalMeta, mergedWarnings);
            }
        }

        /// <summary>Match expression for success/failure branches, allowing consumption of the result.</summary>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        {
            if (onSuccess is null) throw new ArgumentNullException("OnSuccess must not be null.", nameof(onSuccess));
            if (onFailure is null) throw new ArgumentNullException("OnFailure must not be null.", nameof(onFailure));

            return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
        }

        // ----------------------
        // Convenience & utilities
        // ----------------------

        /// <summary>Return true when result is success and all warnings are non-fatal (i.e., Info or Warning severity).</summary>
        public bool HasOnlyNonFatalWarnings()
        {
            if (IsFailure) return false;
            if (Warnings.IsEmpty) return false; // Technically has NO warnings, but usually means there are no FATAL ones.

            foreach (var w in Warnings)
            {
                // Checks for Recoverable (2) or Fatal (3)
                if (w.Severity > Severity.Warning) return false;
            }
            return true;
        }

        /// <summary>Deconstruct the result into a simple tuple form.</summary>
        public void Deconstruct(
            out bool isSuccess,
            out T? value,
            out Error? error,
            out ImmutableArray<Error> warnings,
            out IMetadata operationalMetadata)
        {
            isSuccess = IsSuccess;
            value = _value;
            error = _error;
            warnings = Warnings;
            operationalMetadata = OperationalMetadata;
        }

        /// <summary>Implicit conversion from value to successful <see cref="Result{T}"/>.</summary>
        public static implicit operator Result<T>(T value) => Ok(value);

        /// <summary>Implicit conversion from <see cref="Error"/> to failed <see cref="Result{T}"/>.</summary>
        public static implicit operator Result<T>(Error error) => Fail(error);

        /// <inheritdoc/>
        public override string ToString()
        {
            var warningCount = Warnings.Length;
            var warningText = warningCount > 0 ? $" (Warnings: {warningCount})" : "";
            if (IsSuccess) return $"Ok<{typeof(T).Name}>{warningText}";
            return $"Fail<{typeof(T).Name}> [{Error.CodeKey}] ({Error.Severity}){warningText}";
        }

        // ----------------------
        // Internal helpers
        // ----------------------

        /// <summary>
        /// Normalizes warnings by removing default arrays and sorting them for deterministic order.
        /// </summary>
        private static ImmutableArray<Error> NormalizeWarnings(ImmutableArray<Error> src)
        {
            if (src.IsDefaultOrEmpty) return ImmutableArray<Error>.Empty;

            // High allocation cost due to ToArray() -> Sort() -> CreateRange(). 
            // This cost is justified by ensuring deterministic order for all warning collections.
            var arr = src.ToArray();

            Array.Sort(arr, (a, b) =>
            {
                // Primary sort key: Error Code Key
                var c = string.CompareOrdinal(a.CodeKey, b.CodeKey);
                if (c != 0) return c;

                // Secondary sort key: Error Message
                return string.CompareOrdinal(a.Message, b.Message);
            });

            return ImmutableArray.CreateRange(arr);
        }

        // Debugger property
        private string DebuggerDisplay
        {
            get
            {
                var warnings = Warnings.Length > 0 ? $" | Warnings={Warnings.Length}" : string.Empty;
                if (IsSuccess)
                {
                    var valueStr = _value is null ? "null" : _value.ToString();
                    return $"Ok<{typeof(T).Name}> Value={valueStr}{warnings}";
                }
                return $"Fail<{typeof(T).Name}> Error={_error?.CodeKey ?? "Unknown"}{warnings}";
            }
        }
    }

    // --------------------------------------------------------------------------
    // Result Builder (Refactored for Efficiency)
    // --------------------------------------------------------------------------

    /// <summary>
    /// Lightweight result builder to create rich <see cref="Result{T}"/> instances without intermediate allocations
    /// during the staging phase.
    /// </summary>
    public sealed class ResultBuilder<T>
    {
        private T? _value;
        private Error? _error;
        private Metadata.Metadata.Builder? _metaBuilder;
        private ImmutableArray<Error>.Builder? _warningsBuilder;
        private bool _isFailure;

        /// <summary>Create a new builder instance.</summary>
        public ResultBuilder() { }

        /// <summary>Set the success value on the builder, implicitly setting the state to success.</summary>
        public ResultBuilder<T> WithValue(T value)
        {
            _value = value;
            _isFailure = false;
            _error = null; // Ensure consistency
            return this;
        }

        /// <summary>Set the error on the builder, implicitly setting the state to failure.</summary>
        public ResultBuilder<T> WithError(Error error)
        {
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _isFailure = true;
            _value = default; // Ensure consistency
            return this;
        }

        /// <summary>Adds or updates a single key/value pair to the operational metadata.</summary>
        public ResultBuilder<T> AddOperational(string key, object? value)
        {
            _metaBuilder ??= Metadata.Metadata.NewBuilder();
            _metaBuilder.Set(key, value);
            return this;
        }

        /// <summary>Adds a warning to the builder's collection.</summary>
        public ResultBuilder<T> AddWarning(Error w)
        {
            if (w is null) return this;
            _warningsBuilder ??= ImmutableArray.CreateBuilder<Error>();
            _warningsBuilder.Add(w);
            return this;
        }

        /// <summary>
        /// Builds and returns the final immutable <see cref="Result{T}"/> instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if required fields are missing (e.g., error for failure).</exception>
        public Result<T> Build()
        {
            var warnings = _warningsBuilder?.ToImmutable() ?? ImmutableArray<Error>.Empty;
            var meta = _metaBuilder?.Build() ?? Metadata.Metadata.Empty;

            if (_isFailure)
            {
                if (_error is null) throw new InvalidOperationException("ResultBuilder: error must be set for failure results.");
                return new Result<T>(_error, meta, warnings);
            }

            // Success path
            return new Result<T>(_value!, meta, warnings);
        }
    }

    /// <summary>
    /// Provides static factory methods for creating successful or failed result objects with optional operational
    /// metadata and warnings.
    /// </summary>
    /// <remarks>The Result class offers convenient methods to construct instances of <see cref="Result{T}"/> representing
    /// either successful or failed outcomes. These methods allow callers to supply a value or error, and optionally
    /// attach operational metadata or configure error details using builder actions. All methods return
    /// <see cref="Result{T}"/> instances with no warnings by default. This class is intended for use in scenarios where
    /// operations need to communicate both their outcome and associated metadata in a structured manner.</remarks>
    public static class Result
    {
        // ----------------------
        // Fast-path factories
        // ----------------------

        /// <summary>
        /// Creates a successful <see cref="Result{T}"/> containing the specified value.
        /// </summary>
        /// <typeparam name="T">The type of the value to be contained in the result.</typeparam>
        /// <param name="value">The value to be wrapped in a successful result. Can be <c>null</c> for reference types.</param>
        /// <returns>A <see cref="Result{T}"/> representing a successful operation with the provided value.</returns>
        public static Result<T> Ok<T>(T value) => new(value, Metadata.Metadata.Empty, ImmutableArray<Error>.Empty);

        /// <summary>
        /// Creates a successful result containing the specified value and associated operational metadata.
        /// </summary>
        /// <typeparam name="T">The type of the value to be encapsulated in the result.</typeparam>
        /// <param name="value">The value to be returned as part of the successful result.</param>
        /// <param name="operationalMetadata">The operational metadata to associate with the result. Cannot be null.</param>
        /// <returns>A <see cref="Result{T}"/> representing a successful operation with the provided value and metadata.</returns>
        public static Result<T> Ok<T>(T value, IMetadata operationalMetadata) =>
            new(value, operationalMetadata, ImmutableArray<Error>.Empty);

        /// <summary>
        /// Creates a successful result containing the specified value and optional operational metadata.
        /// </summary>
        /// <remarks>Use this method to attach custom operational metadata to a successful result. If no
        /// metadata configuration is required, pass null for the configureOperationalMetadata parameter.</remarks>
        /// <typeparam name="T">The type of the value to be contained in the result.</typeparam>
        /// <param name="value">The value to include in the successful result.</param>
        /// <param name="configureOperationalMetadata">An action that configures the operational metadata for the result. 
        /// If null, no additional metadata is set.</param>
        /// <returns>A <see cref="Result{T}"/> representing a successful operation with the provided value and configured operational 
        /// metadata.</returns>
        public static Result<T> Ok<T>(T value, Action<Metadata.Metadata.Builder> configureOperationalMetadata)
        {
            if (configureOperationalMetadata is null) return Ok(value);

            var mb = Metadata.Metadata.NewBuilder();
            configureOperationalMetadata(mb);

            return new Result<T>(value, mb.Build(), ImmutableArray<Error>.Empty);
        }

        /// <summary>
        /// Creates a failed result containing the specified error.
        /// </summary>
        /// <typeparam name="T">The type of the value that would be held by a successful result.</typeparam>
        /// <param name="error">The error information to associate with the failed result. Cannot be null.</param>
        /// <returns>A <see cref="Result{T}"/> representing a failed operation with the provided error.</returns>
        public static Result<T> Fail<T>(Error error) => new(error, Metadata.Metadata.Empty, ImmutableArray<Error>.Empty);

        /// <summary>
        /// Creates a failed result containing the specified error and associated operational metadata.
        /// </summary>
        /// <remarks>Use this method to represent an operation that did not succeed and to provide detailed error
        /// information and context for diagnostics or logging.</remarks>
        /// <typeparam name="T">The type of the value that would be held by a successful result.</typeparam>
        /// <param name="error">The error describing the reason for the failure. Cannot be null.</param>
        /// <param name="operationalMetadata">The metadata associated with the failed operation. May provide additional context about the failure.</param>
        /// <returns>A failed <see cref="Result{T}"/> instance containing the specified error and operational metadata.</returns>
        public static Result<T> Fail<T>(Error error, IMetadata operationalMetadata) =>
            new(error, operationalMetadata, ImmutableArray<Error>.Empty);

        /// <summary>
        /// Creates a failed result with a structured error and optional operational metadata.
        /// </summary>
        /// <remarks>Use this method to create a failed result when an operation cannot produce a
        /// successful value. The error is always required and should describe the failure. Operational metadata can be
        /// provided to supply additional context about the failure.</remarks>
        /// <typeparam name="T">The type of the value that would have been returned if the operation succeeded.</typeparam>
        /// <param name="configureError">A delegate that configures the error details using an <see cref="Error.Builder"/>. This parameter cannot be
        /// null.</param>
        /// <param name="configureOperationalMetadata">An optional delegate that configures additional operational metadata using a <see
        /// cref="Metadata.Metadata.Builder"/>. If null, no operational metadata is included.</param>
        /// <returns>A <see cref="Result{T}"/> representing a failed operation, containing the configured error and optional
        /// operational metadata.</returns>
        public static Result<T> Fail<T>(Action<Error.Builder> configureError, Action<Metadata.Metadata.Builder>? configureOperationalMetadata = null)
        {
            var eb = Error.NewBuilder("UNKNOWN_OPERATION_FAILURE", "An operation failed to produce a structured error.");
            configureError?.Invoke(eb);
            var err = eb.Build();

            IMetadata? meta = null;
            if (configureOperationalMetadata is not null)
            {
                var mb = Metadata.Metadata.NewBuilder();
                configureOperationalMetadata(mb);
                meta = mb.Build();
            }

            return new Result<T>(error: err, operationalMetadata: meta, warnings: ImmutableArray<Error>.Empty);
        }
    }
}
